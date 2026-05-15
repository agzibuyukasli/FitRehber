using DietitianClinic.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace DietitianClinic.Tests.Unit;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _connMock = new();
    private readonly Mock<IDatabase>              _dbMock   = new();
    private readonly Mock<IServer>                _srvMock  = new();

    private RedisCacheService CreateSut(int expiryMinutes = 5)
    {
        _connMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                 .Returns(_dbMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Redis:DefaultExpiryMinutes"] = expiryMinutes.ToString()
            })
            .Build();

        return new RedisCacheService(_connMock.Object, NullLogger<RedisCacheService>.Instance, config);
    }

    // ─── GetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyIsNullOrEmpty()
    {
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);

        var sut    = CreateSut();
        var result = await sut.GetAsync<string>("missing_key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsDeserializedValue_WhenKeyExists()
    {
        var expected   = new { Name = "FitRehber", Version = 1 };
        var serialized = JsonSerializer.Serialize(expected);

        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((RedisValue)serialized);

        var sut    = CreateSut();
        var result = await sut.GetAsync<JsonElement>("test_key");

        Assert.Equal("FitRehber", result.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenRedisThrowsException()
    {
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test"));

        var sut    = CreateSut();
        var result = await sut.GetAsync<string>("key");

        Assert.Null(result);
    }

    // ─── SetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_CallsStringSet_WithSerializedValue()
    {
        _dbMock.Setup(d => d.StringSetAsync(
                   It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                   It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
                   It.IsAny<When>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.SetAsync("key", "value", TimeSpan.FromMinutes(1));

        _dbMock.Verify(
            d => d.StringSetAsync(
                "key", It.Is<RedisValue>(v => v.ToString().Contains("value")),
                TimeSpan.FromMinutes(1), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_UsesDefaultExpiry_WhenNullPassed()
    {
        _dbMock.Setup(d => d.StringSetAsync(
                   It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                   It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
                   It.IsAny<When>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        var sut = CreateSut(expiryMinutes: 10);
        await sut.SetAsync("k", "v", expiry: null);

        _dbMock.Verify(
            d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                TimeSpan.FromMinutes(10), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_DoesNotThrow_WhenRedisThrowsException()
    {
        _dbMock.Setup(d => d.StringSetAsync(
                   It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                   It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
                   It.IsAny<When>(), It.IsAny<CommandFlags>()))
               .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test"));

        var sut = CreateSut();
        var ex  = await Record.ExceptionAsync(() => sut.SetAsync("k", "v"));

        Assert.Null(ex);
    }

    // ─── RemoveAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_CallsKeyDelete()
    {
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.RemoveAsync("key_to_delete");

        _dbMock.Verify(d => d.KeyDeleteAsync("key_to_delete", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_DoesNotThrow_WhenRedisThrowsException()
    {
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test"));

        var sut = CreateSut();
        var ex  = await Record.ExceptionAsync(() => sut.RemoveAsync("key"));

        Assert.Null(ex);
    }

    // ─── RemoveByPrefixAsync ──────────────────────────────────────────

    [Fact]
    public async Task RemoveByPrefix_ReturnsEarly_WhenNoServersAvailable()
    {
        _connMock.Setup(c => c.GetServers()).Returns(Array.Empty<IServer>());

        var sut = CreateSut();
        var ex  = await Record.ExceptionAsync(() => sut.RemoveByPrefixAsync("patients:"));

        Assert.Null(ex);
        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByPrefix_DeletesMatchingKeys_WhenKeysExist()
    {
        // Bu test, "await Db.KeyDeleteAsync(keys)" satırını kapsar (1 uncovered line)
        _connMock.Setup(c => c.GetServers()).Returns(new[] { _srvMock.Object });
        _srvMock.Setup(s => s.Keys(
                    It.IsAny<int>(), It.IsAny<RedisValue>(),
                    It.IsAny<int>(), It.IsAny<long>(),
                    It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(new RedisKey[] { "patients:1", "patients:2" });
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(2L);

        var sut = CreateSut();
        await sut.RemoveByPrefixAsync("patients:");

        _dbMock.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey[]>(k => k.Length == 2),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByPrefix_DoesNotCallDelete_WhenNoKeysMatch()
    {
        _connMock.Setup(c => c.GetServers()).Returns(new[] { _srvMock.Object });
        _srvMock.Setup(s => s.Keys(
                    It.IsAny<int>(), It.IsAny<RedisValue>(),
                    It.IsAny<int>(), It.IsAny<long>(),
                    It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Array.Empty<RedisKey>());

        var sut = CreateSut();
        await sut.RemoveByPrefixAsync("patients:");

        _dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByPrefix_DoesNotThrow_WhenRedisThrowsException()
    {
        _connMock.Setup(c => c.GetServers()).Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test"));

        var sut = CreateSut();
        var ex  = await Record.ExceptionAsync(() => sut.RemoveByPrefixAsync("any:"));

        Assert.Null(ex);
    }
}
