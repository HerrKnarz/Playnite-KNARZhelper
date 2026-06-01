using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace KNARZhelper.ScreenshotsCommon.Models
{
    public enum MediaType
    {
        Unknown = 0,
        Screenshot = 1,
        Background = 2,
        Advertisement = 3,
        Banner = 4,
        BoxFront = 5,
        BoxBack = 6,
        BoxSpine = 7,
        Box3D = 8,
        Logo = 9,
        Disc = 10,
        Cartridge = 11,
        ArcadeCabinet = 12,
        ArcadeCircuit = 13,
        ArcadeControlPanel = 14,
        ArcadeControlsInfo = 15,
        ArcadeMarquee = 16,
        Icon = 17,
        Poster = 18,
        Decal = 19,
        Artwork = 20,
        Promoshot = 21,
        SelfmadeScreenshot = 22,
        Manual = 23,
        PromoVideo = 24,
        PrivateVideo = 25,
    }

    /// <summary>
    /// Dictionary of types with captions to show in a combo box.
    /// </summary>
    public class MediaTypesWithCaptions : Dictionary<MediaType, string>
    {
        public MediaTypesWithCaptions()
        {
            Add(MediaType.Unknown, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeUnknown"));
            Add(MediaType.Screenshot, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeScreenshot"));
            Add(MediaType.SelfmadeScreenshot, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeSelfmadeScreenshot"));
            Add(MediaType.Promoshot, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypePromoshot"));
            Add(MediaType.Background, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeBackground"));
            Add(MediaType.Icon, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeIcon"));
            Add(MediaType.Logo, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeLogo"));
            Add(MediaType.BoxFront, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeBoxFront"));
            Add(MediaType.BoxBack, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeBoxBack"));
            Add(MediaType.BoxSpine, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeBoxSpine"));
            Add(MediaType.Box3D, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeBox3D"));
            Add(MediaType.Disc, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeDisc"));
            Add(MediaType.Cartridge, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeCartridge"));
            Add(MediaType.ArcadeCabinet, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeArcadeCabinet"));
            Add(MediaType.ArcadeCircuit, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeArcadeCircuit"));
            Add(MediaType.ArcadeControlPanel, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeArcadeControlPanel"));
            Add(MediaType.ArcadeControlsInfo, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeArcadeControlsInfo"));
            Add(MediaType.ArcadeMarquee, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeArcadeMarquee"));
            Add(MediaType.Banner, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeBanner"));
            Add(MediaType.Poster, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypePoster"));
            Add(MediaType.Decal, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeDecal"));
            Add(MediaType.Advertisement, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeAdvertisement"));
            Add(MediaType.Artwork, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeArtwork"));
            Add(MediaType.Manual, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypeManual"));
            Add(MediaType.PromoVideo, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypePromoVideo"));
            Add(MediaType.PrivateVideo, ResourceProvider.GetString("LOCScreenshotUtilitiesMediaTypePrivateVideo"));
        }
    }

    /// <summary>
    /// Class representing a screenshot with properties and methods for managing it.
    /// </summary>
    public partial class Screenshot : ObservableObject
    {
        private string _description;
        private string _downloadedPath;
        private string _downloadedThumbnailPath;
        private Guid _id = Guid.NewGuid();
        private string _name;
        private string _path;
        private int _sortOrder = 0;
        private string _thumbnailPath;
        private MediaType _type = MediaType.Promoshot;

        /// <summary>
        /// Creates a new instance of the Screenshot class.
        /// </summary>
        /// <param name="path">Path to the screenshot file.</param>
        /// <param name="name">Name of the screenshot.</param>
        /// <param name="id">Unique identifier for the screenshot.</param>
        public Screenshot(string path = "", string name = "", Guid id = default)
        {
            _id = id == default ? _id : id;
            _path = path;
            _name = name;
        }

        /// <summary>
        /// Description of the screenshot.
        /// </summary>
        [SerializationPropertyName("description")]
        public string Description
        {
            get => _description;
            set => SetValue(ref _description, value);
        }

        /// <summary>
        /// Gets the display path for the screenshot. If the screenshot is downloaded, it returns
        /// the downloaded path; otherwise, it returns the original path.
        /// </summary>
        [DontSerialize]
        public string DisplayPath => IsDownloaded ? DownloadedPath : Path;

        /// <summary>
        /// Gets the path to the thumbnail image to display, based on the download and availability status.
        /// </summary>
        [DontSerialize]
        public string DisplayThumbnail => !string.IsNullOrEmpty(DownloadedThumbnailPath)
            ? DownloadedThumbnailPath : !string.IsNullOrEmpty(ThumbnailPath)
            ? ThumbnailPath : DisplayPath;

        /// <summary>
        /// Gets or sets the file path where the downloaded screenshot is stored.
        /// </summary>
        [SerializationPropertyName("downloadedPath")]
        public string DownloadedPath
        {
            get => _downloadedPath;
            set => SetValue(ref _downloadedPath, value);
        }

        /// <summary>
        /// Gets or sets the file path of the thumbnail to the downloaded screenshot. It will be
        /// generated automatically after downloading the screenshot from the original file instead
        /// of downloading the thumbnail separately.
        /// </summary>
        [SerializationPropertyName("downloadedThumbnailPath")]
        public string DownloadedThumbnailPath
        {
            get => _downloadedThumbnailPath;
            set => SetValue(ref _downloadedThumbnailPath, value);
        }

        /// <summary>
        /// Gets or sets the unique identifier for the screenshot. It is used as the filename for
        /// the downloaded image. Becaus of than it's not advisable to change this value after the
        /// screenshot was downloaded already to avoid confusion.
        /// </summary>
        [SerializationPropertyName("id")]
        public Guid Id
        {
            get => _id;
            set => SetValue(ref _id, value);
        }

        /// <summary>
        /// Specifies whether the screenshot has been downloaded.
        /// </summary>
        [DontSerialize]
        public bool IsDownloaded => !string.IsNullOrEmpty(DownloadedPath);

        /// <summary>
        /// Determines if the media type of the screenshot is a video (e.g., PromoVideo or
        /// PrivateVideo). We return false if the path is a https url, since the MedtaElement can't
        /// play those and crashes Playnite in the process.
        /// </summary>
        [DontSerialize]
        public bool IsVideo => _type.IsOneOf(MediaType.PromoVideo, MediaType.PrivateVideo) && !DownloadedPath.StartsWith("https");

        /// <summary>
        /// Name of the screenshot.
        /// </summary>
        [SerializationPropertyName("name")]
        public string Name
        {
            get => _name;
            set => SetValue(ref _name, value);
        }

        /// <summary>
        /// Initial path or URL of the screenshot.
        /// </summary>
        [SerializationPropertyName("path")]
        public string Path
        {
            get => _path;
            set => SetValue(ref _path, value);
        }

        /// <summary>
        /// Specifies whether the Path property is a valid URL.
        /// </summary>
        [DontSerialize]
        public bool PathIsUrl => !string.IsNullOrEmpty(Path) && Path.IsValidHttpUrl();

        /// <summary>
        /// Gets or sets the sort order for the item.
        /// </summary>
        [SerializationPropertyName("sortOrder")]
        public int SortOrder
        {
            get => _sortOrder;
            set => SetValue(ref _sortOrder, value);
        }

        /// <summary>
        /// Gets or sets the initial path or URL to the thumbnail image.
        /// </summary>
        [SerializationPropertyName("thumbnailPath")]
        public string ThumbnailPath
        {
            get => _thumbnailPath;
            set => SetValue(ref _thumbnailPath, value);
        }

        /// <summary>
        /// Gets or sets the media type associated with the content.
        /// </summary>
        [SerializationPropertyName("type")]
        public MediaType Type
        {
            get => _type;
            set => SetValue(ref _type, value);
        }

        /// <summary>
        /// Determines if the media type of the screenshot is a video (e.g., PromoVideo or PrivateVideo).
        /// </summary>
        [DontSerialize]
        public string VideoPath => _type.IsOneOf(MediaType.PromoVideo, MediaType.PrivateVideo) ? Path : null;
    }
}
