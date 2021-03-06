﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcoustID;
using AcoustID.Web;
using LibraryBridger.Generic;

namespace LibraryBridger.Fingerprinting
{
    public class DetectionOperations
    {
        public string GenerateFingerprint(string file)
        {
            var decoder = new NAudioDecoder(file);
            var context = new ChromaContext();

            context.Start(decoder.SampleRate, decoder.Channels);
            decoder.Decode(context.Consumer, 1000);
            context.Finish();

            return context.GetFingerprint();
        }
        
        public LocalTrack LookUpFingerprint(string path)
        {
            if (String.IsNullOrEmpty(AcoustID.Configuration.ClientKey))
            {
                Configuration.ClientKey = "7qnNawP8nH";
            }
            LookupService service = new LookupService();
            LocalTrack track = new LocalTrack();
            var fingerprint = GenerateFingerprint(path);
            var file = TagLib.File.Create(path);
            var duration = (int)file.Properties.Duration.TotalSeconds;
            file.Dispose();
            var context = TaskScheduler.Default;
            var task = service.GetAsync(fingerprint, duration, 
                new string[] { "recordings", "compress" });
            task.Wait();
            // Error handling:
            var successContinuation = task.ContinueWith(t =>
            {
                foreach (var e in t.Exception.InnerExceptions)
                {
                    track.Error = "Webservice error";
                }
            },
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | 
            TaskContinuationOptions.ExecuteSynchronously, context);

            // On success:
            var failureContinuation = task.ContinueWith(t =>
            {
                track.Error = "Something went wrong";
                var response = t.Result;

                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    track.Error = "Webservice error";
                    return;
                }

                if (response.Results.Count == 0)
                {
                    track.Error = "No results for given fingerprint.";
                    return;
                }

                foreach (var result in response.Results)
                {
                    if(result.Recordings.Count != 0)
                    {
                        track.Error = null;
                        track.Author = result.Recordings[0].Artists[0].Name;
                        track.Title = result.Recordings[0].Title;
                        track.TagState = TagState.FULL_TAGS;
                    }
                }
            },
            CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion | 
            TaskContinuationOptions.ExecuteSynchronously, context);
            Task.WaitAny(successContinuation, failureContinuation);
            return track;
        }

        public void SubmitFingerprint(string path)
        {
            var fingerprint = GenerateFingerprint(path);
            var file = TagLib.File.Create(path);
            var duration = (int)file.Properties.Duration.TotalSeconds;
            file.Dispose();

            SubmitService service = new SubmitService("OL52LsoEE3");
            SubmitRequest request = new SubmitRequest(fingerprint, duration)
            {
                Album = file.Tag.Album,
                Artist = file.Tag.FirstPerformer,
                Title = file.Tag.Title,
                AlbumArtist = file.Tag.FirstAlbumArtist
            };

            var task = service.SubmitAsync(request);
            task.Wait();
        }
    }
}
