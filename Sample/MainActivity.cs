using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Com.Coremedia.Iso;
using Com.Coremedia.Iso.Boxes;
using Com.Googlecode.Mp4parser;
using Com.Googlecode.Mp4parser.Authoring;
using Com.Googlecode.Mp4parser.Authoring.Builder;
using Java.IO;
using Java.Nio.Channels;
using System.Collections;
using System.IO;
using Xamarin.Essentials;
using Movie = Com.Googlecode.Mp4parser.Authoring.Movie;

namespace Sample {
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity {

        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Platform.Init(this, savedInstanceState);

            RequestPermission();
        }

        public async void RequestPermission() {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted) {
                await Permissions.RequestAsync<Permissions.StorageWrite>();
            }

            FixVideoFileIfNecessary();
        }

        public static string FixVideoFileIfNecessary() {
            Java.IO.File videoFile = new Java.IO.File(Path.Combine(Environment.ExternalStorageDirectory.ToString(), "video_to_fix.mp4"));

            using (IDataSource channel = new FileDataSourceImpl(videoFile.AbsolutePath)) {
                IsoFile isoFile = new IsoFile(channel);

                IList trackBoxes = isoFile.MovieBox.GetBoxes(Java.Lang.Class.FromType(typeof(TrackBox)));

                bool hasError = true;

                foreach (TrackBox trackBox in trackBoxes) {
                    TimeToSampleBox.Entry firstEntry = trackBox.MediaBox.MediaInformationBox.SampleTableBox.TimeToSampleBox.Entries[0];

                    if (firstEntry.Delta > 10000) {
                        hasError = true;
                        firstEntry.Delta = 3000;
                    }
                }

                if (hasError) {
                    Movie movie = new Movie();

                    foreach (TrackBox trackBox in trackBoxes) {
                        movie.AddTrack(new Mp4TrackImpl(channel.ToString() + "[" + trackBox.TrackHeaderBox.TrackId + "]", trackBox));
                    }

                    movie.Matrix = isoFile.MovieBox.MovieHeaderBox.Matrix;

                    using IContainer outt = new DefaultMp4Builder().Build(movie);

                    string filePath = Path.Combine(Environment.ExternalStorageDirectory.ToString(), "fixedVideo.mp4");

                    using FileChannel fileChannel = new RandomAccessFile(filePath, "rw").Channel;
                    outt.WriteContainer(fileChannel);

                    return filePath;
                }
            }

            return videoFile.AbsolutePath;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}