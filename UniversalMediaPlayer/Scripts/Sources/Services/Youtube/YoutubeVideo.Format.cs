namespace UMP.Services.Youtube
{
    public partial class YoutubeVideo
    {
        public int Fps
        {
            get
            {
                switch (FormatCode)
                {
                    case 571:
                    case 402:
                    case 401:
                    case 400:
                    case 399:
                    case 398:
                    case 337:
                    case 336:
                    case 335:
                    case 334:
                    case 333:
                    case 332:
                    case 331:
                    case 330:
                    case 272:
                    case 315:
                    case 308:
                    case 303:
                    case 302:
                    case 305:
                    case 304:
                    case 299:
                    case 298:
                        return 60;
                    case 18:
                    case 22:
                    case 37:
                    case 43:
                    case 59:
                    case 397:
                    case 396:
                    case 395:
                    case 394:
                    case 313:
                    case 271:
                    case 248:
                    case 247:
                    case 244:
                    case 243:
                    case 242:
                    case 278:
                    case 138:
                    case 266:
                    case 264:
                    case 137:
                    case 136:
                    case 135:
                    case 134:
                    case 133:
                    case 160:
                        return 30;
                    default:
                        return -1;
                }
            }
        }

        public bool Is3D
        {
            get
            {
                switch (FormatCode)
                {
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 100:
                    case 101:
                    case 102:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsAdaptive
        {
            get { return AdaptiveType != AdaptiveFormat.None; }
        }

        public AdaptiveFormat AdaptiveType
        {
            get
            {
                switch (FormatCode)
                {
                    case 18:
                    case 22:
                    case 37:
                    case 43:
                    case 59:
                    case 133:
                    case 134:
                    case 135:
                    case 136:
                    case 137:
                    case 138:
                    case 160:
                    case 242:
                    case 243:
                    case 244:
                    case 247:
                    case 248:
                    case 264:
                    case 266:
                    case 271:
                    case 272:
                    case 278:
                    case 298:
                    case 299:
                    case 302:
                    case 303:
                    case 304:
                    case 305:
                    case 308:
                    case 313:
                    case 315:
                    case 330:
                    case 331:
                    case 332:
                    case 333:
                    case 334:
                    case 335:
                    case 336:
                    case 337:
                    case 394:
                    case 395:
                    case 396:
                    case 397:
                    case 398:
                    case 399:
                    case 400:
                    case 401:
                    case 402:
                    case 571:
                        return AdaptiveFormat.Video;
                    case 139:
                    case 140:
                    case 141:
                    case 171:
                    case 172:
                    case 249:
                    case 250:
                    case 251:
                    case 256:
                    case 258:
                    case 327:
                    case 338:
                        return AdaptiveFormat.Audio;
                    default:
                        return AdaptiveFormat.None;
                }
            }
        }

        public int AudioBitrate
        {
            get
            {
                switch (FormatCode)
                {
                    case 17:
                        return 24;
                    case 36:
                        return 38;
                    case 139:
                    case 249:
                    case 250:
                        return 48;
                    case 5:
                    case 6:
                        return 64;
                    case 18:
                    case 82:
                    case 83:
                        return 96;
                    case 34:
                    case 35:
                    case 37:
                    case 43:
                    case 44:
                    case 59:
                    case 100:
                    case 140:
                    case 171:
                    case 251:
                        return 128;
                    case 84:
                    case 85:
                        return 152;
                    case 22:
                    case 38:
                    case 45:
                    case 46:
                    case 101:
                    case 102:
                    case 256:
                        return 192;
                    case 141:
                    case 172:
                    case 327:
                        return 256;
                    case 258:
                        return 384;
                    case 338:
                        return 480;
                    default:
                        return -1;
                }
            }
        }

        public int Resolution
        {
            get
            {
                switch (FormatCode)
                {
                    case 6:
                        return 270;
                    case 17:
                    case 394:
                    case 330:
                    case 278:
                    case 160:
                        return 144;
                    case 5:
                    case 36:
                    case 83:
                    case 395:
                    case 331:
                    case 242:
                    case 133:
                        return 240;
                    case 18:
                    case 34:
                    case 43:
                    case 82:
                    case 100:
                    case 101:
                    case 396:
                    case 332:
                    case 243:
                    case 134:
                        return 360;
                    case 35:
                    case 44:
                    case 59:
                    case 397:
                    case 333:
                    case 244:
                    case 135:
                        return 480;
                    case 22:
                    case 398:
                    case 334:
                    case 302:
                    case 45:
                    case 84:
                    case 102:
                    case 247:
                    case 298:
                    case 136:
                        return 720;
                    case 37:
                    case 46:
                    case 399:
                    case 335:
                    case 303:
                    case 248:
                    case 299:
                    case 137:
                        return 1080;
                    case 38:
                        return 3072; // what
                    case 85:
                        return 520;
                    case 400:
                    case 336:
                    case 308:
                    case 271:
                    case 304:
                    case 264:
                        return 1440;
                    case 401:
                    case 337:
                    case 315:
                    case 313:
                    case 305:
                    case 266:
                        return 2160;
                    case 138:
                    case 272:
                    case 402:
                    case 571:
                        return 4320;
                    default:
                        return -1;
                }
            }
        }

        public override VideoFormat VideoFormat
        {
            get
            {
                switch (FormatCode)
                {
                    case 5:
                    case 6:
                    case 34:
                    case 35:
                        return VideoFormat.Flv;
                    case 13:
                    case 17:
                    case 36:
                        return VideoFormat.Mobile;
                    case 18:
                    case 22:
                    case 37:
                    case 38:
                    case 59:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 133:
                    case 134:
                    case 135:
                    case 136:
                    case 137:
                    case 138:
                    case 139:
                    case 140:
                    case 141:
                    case 160:
                    case 264:
                    case 266:
                    case 298:
                    case 299:
                    case 304:
                    case 305:
                    case 394:
                    case 395:
                    case 396:
                    case 397:
                    case 398:
                    case 399:
                    case 400:
                    case 401:
                    case 402:
                    case 571:
                        return VideoFormat.Mp4;
                    case 43:
                    case 44:
                    case 45:
                    case 46:
                    case 100:
                    case 101:
                    case 102:
                    case 242:
                    case 243:
                    case 244:
                    case 247:
                    case 248:
                    case 271:
                    case 272:
                    case 278:
                    case 171:
                    case 172:
                    case 249:
                    case 250:
                    case 251:
                    case 302:
                    case 303:
                    case 308:
                    case 313:
                    case 315:
                    case 330:
                    case 331:
                    case 332:
                    case 333:
                    case 334:
                    case 335:
                    case 336:
                    case 337:
                        return VideoFormat.WebM;
                    default:
                        return VideoFormat.Unknown;
                }
            }
        }

        public override AudioFormat AudioFormat
        {
            get
            {
                switch (FormatCode)
                {
                    case 5:
                    case 6:
                        return AudioFormat.Mp3;
                    case 13:
                    case 17:
                    case 18:
                    case 22:
                    case 34:
                    case 35:
                    case 36:
                    case 37:
                    case 38:
                    case 59:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 139:
                    case 140:
                    case 141:
                    case 256:
                    case 258:
                    case 327:
                        return AudioFormat.Aac;
                    //case 43:
                    case 44:
                    case 45:
                    case 46:
                    case 100:
                    case 101:
                    case 102:
                    case 171:
                    case 172:
                        return AudioFormat.Vorbis;
                    case 43:
                    case 249:
                    case 250:
                    case 251:
                    case 338:
                        return AudioFormat.Opus;
                    default:
                        return AudioFormat.Unknown;
                }
            }
        }
    }
}
