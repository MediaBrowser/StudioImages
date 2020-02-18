using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using System;
using MediaBrowser.Common.Progress;
using MediaBrowser.Model.Configuration;

namespace StudioImages
{
    public class StudiosImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public StudiosImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "Emby Designs"; }
        }

        public bool Supports(BaseItem item)
        {
            return item is Studio;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            return GetImages(item, true, true, cancellationToken);
        }

        private async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, bool posters, bool thumbs, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (posters)
            {
                const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studioposters.txt";

                using (var response = await GetList(url, _httpClient, _fileSystem, cancellationToken).ConfigureAwait(false))
                {
                    using (var stream = response.Content)
                    {
                        var image = await GetImage(item, stream, ImageType.Primary, "folder").ConfigureAwait(false);
                        if (image != null)
                        {
                            list.Add(image);
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (thumbs)
            {
                const string url = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studiothumbs.txt";

                using (var response = await GetList(url, _httpClient, _fileSystem, cancellationToken).ConfigureAwait(false))
                {
                    using (var stream = response.Content)
                    {
                        var image = await GetImage(item, stream, ImageType.Thumb, "thumb").ConfigureAwait(false);
                        if (image != null)
                        {
                            list.Add(image);
                        }
                    }
                }
            }

            return list;
        }

        private async Task<RemoteImageInfo> GetImage(BaseItem item, Stream stream, ImageType type, string remoteFilename)
        {
            var list = await GetAvailableImages(stream, _fileSystem).ConfigureAwait(false);

            var match = FindMatch(item, list);

            if (!string.IsNullOrEmpty(match))
            {
                var url = GetUrl(match, remoteFilename);

                return new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = type,
                    Url = url
                };
            }

            return null;
        }

        private string GetUrl(string image, string filename)
        {
            return string.Format("https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname/studios/{0}/{1}.jpg", image, filename);
        }


        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                BufferContent = false
            });
        }

        /// <summary>
        /// Ensures the list.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="file">The file.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task<HttpResponseInfo> GetList(string url, IHttpClient httpClient, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            return httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Progress = new SimpleProgress<double>(),
                Url = url,
                CacheLength = TimeSpan.FromDays(1),
                CacheMode = CacheMode.Unconditional
            });
        }

        public string FindMatch(BaseItem item, IEnumerable<string> images)
        {
            var name = GetComparableName(item.Name);

            return images.FirstOrDefault(i => string.Equals(name, GetComparableName(i), StringComparison.OrdinalIgnoreCase));
        }

        private string GetComparableName(string name)
        {
            return name.Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace("&", string.Empty)
                .Replace("!", string.Empty)
                .Replace(",", string.Empty)
                .Replace("/", string.Empty);
        }

        public async Task<List<string>> GetAvailableImages(Stream stream, IFileSystem fileSystem)
        {
            using (var reader = new StreamReader(stream))
            {
                var lines = new List<string>();

                while (!reader.EndOfStream)
                {
                    var text = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        lines.Add(text);
                    }
                }

                return lines;
            }
        }

    }
}
