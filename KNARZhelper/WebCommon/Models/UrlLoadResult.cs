using HtmlAgilityPack;
using Playnite.SDK;
using System.Net;
using System.Windows.Media;

namespace KNARZhelper.WebCommon.Models
{
    public class UrlLoadResult
    {
        // TODO: Remove HTMLAgilityPack and only return the text.
        public HtmlDocument Document { get; set; } = null;

        public string ErrorDetails { get; set; } = string.Empty;
        public string PageText { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public string ResponseUrl { get; set; } = string.Empty;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Unused;

        public SolidColorBrush StatusColor => StatusCode >= HttpStatusCode.OK && StatusCode < HttpStatusCode.Ambiguous
            ? (SolidColorBrush)ResourceProvider.GetResource("PositiveRatingBrush")
            : StatusCode >= HttpStatusCode.Ambiguous && StatusCode < HttpStatusCode.BadRequest
                ? (SolidColorBrush)ResourceProvider.GetResource("MixedRatingBrush")
                : (SolidColorBrush)ResourceProvider.GetResource("NegativeRatingBrush");
    }
}