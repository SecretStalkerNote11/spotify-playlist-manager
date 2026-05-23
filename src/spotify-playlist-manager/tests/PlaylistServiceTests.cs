using Moq;
using SpotifyAPI.Web;
using SpotifyPlaylistManager.Core;

namespace SpotifyPlaylistManager.Tests;

/// <summary>
/// Unit tests for the PlaylistService class.
/// </summary>
public class PlaylistServiceTests
{
    private readonly Mock<SpotifyClient> _mockSpotify;
    private readonly PlaylistService _service;

    public PlaylistServiceTests()
    {
        _mockSpotify = new Mock<SpotifyClient>();
        _service = new PlaylistService(_mockSpotify.Object);
    }

    [Fact]
    public async Task GetUserPlaylistsAsync_ShouldReturnPlaylists()
    {
        // Arrange
        var expectedPlaylists = new List<SimplePlaylist>
        {
            new SimplePlaylist { Id = "1", Name = "Test Playlist" }
        };

        var pagingResponse = new Paging<SimplePlaylist>
        {
            Items = expectedPlaylists,
            Next = null
        };

        _mockSpotify.Setup(s => s.Playlists.CurrentUsers(It.IsAny<PlaylistsRequest>()))
            .ReturnsAsync(pagingResponse);

        // Act
        var result = await _service.GetUserPlaylistsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result[0].Id);
        Assert.Equal("Test Playlist", result[0].Name);
    }

    [Fact]
    public async Task TransferPlaylistAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var playlistId = "original123";
        var targetUserId = "user456";

        var fullPlaylist = new FullPlaylist
        {
            Id = playlistId,
            Name = "My Playlist",
            Description = "A test playlist",
            Public = false
        };

        var newPlaylist = new FullPlaylist
        {
            Id = "new789",
            Name = "My Playlist"
        };

        _mockSpotify.Setup(s => s.Playlists.Get(playlistId))
            .ReturnsAsync(fullPlaylist);

        _mockSpotify.Setup(s => s.Playlists.Create(targetUserId, It.IsAny<PlaylistCreateRequest>()))
            .ReturnsAsync(newPlaylist);

        var trackPaging = new Paging<PlaylistTrack<IPlayableItem>>
        {
            Items = new List<PlaylistTrack<IPlayableItem>>(),
            Next = null
        };

        _mockSpotify.Setup(s => s.Playlists.GetItems(playlistId, It.IsAny<PlaylistGetItemsRequest>()))
            .ReturnsAsync(trackPaging);

        // Act
        var result = await _service.TransferPlaylistAsync(playlistId, targetUserId);

        // Assert
        Assert.True(result);
        _mockSpotify.Verify(s => s.Playlists.Create(targetUserId, It.IsAny<PlaylistCreateRequest>()), Times.Once);
    }

    [Fact]
    public async Task TransferPlaylistAsync_ShouldReturnFalseOnApiException()
    {
        // Arrange
        var playlistId = "bad123";
        var targetUserId = "user456";

        _mockSpotify.Setup(s => s.Playlists.Get(playlistId))
            .ThrowsAsync(new APIException("Not found", 404, ""));

        // Act
        var result = await _service.TransferPlaylistAsync(playlistId, targetUserId);

        // Assert
        Assert.False(result);
    }
}
