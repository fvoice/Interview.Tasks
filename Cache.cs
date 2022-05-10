using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Interview.Tasks
{
	/// <summary>
	/// You have a class implementing a simple caching logic:
	/// </summary>

	public interface IRepository
	{
		Task<List<string>> Get();
	}

	public class Repository : IRepository
	{
		public async Task<List<string>> Get()
		{
			await Task.Delay(100);
			return new List<string>() { "1", "2", "3" };
		}
	}

	public class Cache
	{
		private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(10);
		private DateTime _lastCacheUpdateTime = DateTime.Now;
		private List<string> _values;

		private readonly IRepository _repo;

		public Cache(IRepository repo)
		{
			_repo = repo;
		}

		public async Task<List<string>> Get()
		{
			if (DateTime.Now - _lastCacheUpdateTime > _cacheDuration || _values == null || !_values.Any())
			{
				await RefreshCache();
				_lastCacheUpdateTime = DateTime.Now;
			}
			return _values;
		}

		private async Task RefreshCache()
		{
			//very expensive operation to get the values
			await _repo.Get();
		}
	}

	/// <summary>
	/// but the issue is that updating of the cache values is a very expensive operation. Your web site handles thousands requests per second and every time the cache expires, plenty requests to DB are sent. Your task is to change the Cache class to meet the following requirements:
	///- no user should receive outdated(expired) information from the cache
	///- when cache expires the only one request should be sent to the DB to update the cache
	///Write a unit test to check that the new implementation works in tough concurrent environment
	///Solution:
	/// </summary>

	public class Cache2
	{
		private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);

		private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(100);
		private DateTime _lastCacheUpdateTime = DateTime.Now;
		private List<string> _values;

		private readonly IRepository _repo;

		public Cache2(IRepository repo)
		{
			_repo = repo;
		}

		public async Task<List<string>> Get()
		{
			if (DateTime.Now - _lastCacheUpdateTime > _cacheDuration || _values == null || !_values.Any())
			{
				await Semaphore.WaitAsync();
				try
				{
					if (DateTime.Now - _lastCacheUpdateTime > _cacheDuration || _values == null || !_values.Any())
					{
						await RefreshCache();
						_lastCacheUpdateTime = DateTime.Now;
					}
				}
				finally
				{
					Semaphore.Release();
				}
			}
			return _values;
		}

		private async Task RefreshCache()
		{
			//very expensive operation to get the values
			_values = await _repo.Get();

		}
	}

	[TestFixture]
	public class CacheTest
	{
		[Test]
		public void SaveShouldPerform3Attempts()
		{
			//arrange
			int counter = 0;

			var repoMock = new Mock<IRepository>();
			repoMock.Setup(x => x.Get())
				.Callback(() =>
				{
					//will increase the counter and generate and exception
					counter++;
				})
				.ReturnsAsync(new List<string>() {"1"});

			var sut = new Cache2(repoMock.Object);

			//act
			List<Task> tasks = new List<Task>();
			for (int i = 0; i < 10; i++)
			{
				tasks.Add(Task.Run(() => sut.Get()));
			}

			Task.WaitAll(tasks.ToArray());

			//assert
			Assert.AreEqual(1, counter);
		}
	}
}
