#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicArtistResolver.
    /// </summary>
    public class MusicArtistResolver : ItemResolver<MusicArtist>
    {
        private readonly ILogger<MusicAlbumResolver> _logger;
        private NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicArtistResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger for the created <see cref="MusicAlbumResolver"/> instances.</param>
        /// <param name="namingOptions">The naming options.</param>
        public MusicArtistResolver(
            ILogger<MusicAlbumResolver> logger,
            NamingOptions namingOptions)
        {
            _logger = logger;
            _namingOptions = namingOptions;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Second;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicArtist.</returns>
        protected override MusicArtist Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory)
            {
                return null;
            }

            // Don't allow nested artists
            if (args.HasParent<MusicArtist>() || args.HasParent<MusicAlbum>())
            {
                return null;
            }

            var collectionType = args.GetCollectionType();

            var isMusicMediaFolder = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

            // If there's a collection type and it's not music, it can't be a series
            if (!isMusicMediaFolder)
            {
                return null;
            }

            if (args.ContainsFileSystemEntryByName("artist.nfo"))
            {
                return new MusicArtist();
            }

            // Avoid mis-identifying top folders
            if (args.Parent.IsRoot)
            {
                return null;
            }

            var directoryService = args.DirectoryService;

            var albumResolver = new MusicAlbumResolver(_logger, _namingOptions);

            // If we contain an album assume we are an artist folder
            var directories = args.FileSystemChildren.Where(i => i.IsDirectory);

            var result = Parallel.ForEach(directories, (fileSystemInfo, state) =>
            {
                if (albumResolver.IsMusicAlbum(fileSystemInfo.FullName, directoryService))
                {
                    // stop once we see a music album
                    state.Stop();
                }
            });

            return !result.IsCompleted ? new MusicArtist() : null;
        }
    }
}
