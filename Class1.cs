using NUnit.Framework;
using System;
using Moq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	/// <summary>
	/// There are two interfaces IRepository and IService
	/// There is an repository implementation
	/// The task is to write an implementation of a service, using this repository to store data
	/// Requirements: 
	/// 1. If an Exception happens, the service have to re-try saving 2 more times and re-throw the exception in case of failure
	/// 2. Write an unit tests testing the requirement
	/// 3. The service should be thread-safe
	/// </summary>
	public interface IRepository
	{
		Task Save(object obj);
	}

	public interface IService
	{
		Task Save(object obj);
	}

	public class Repository : IRepository
	{
		public async Task Save(object obj)
		{
			await Task.Delay(100);
		}
	}

	/// <summary>
	/// end
	/// </summary>

	public class Service : IService
	{
		private IRepository _repository;

		public Service(IRepository repository)
		{
			_repository = repository;
		}

		public async Task Save(object obj)
		{
			await SaveInternal(obj, 1);
		}

		public async Task SaveInternal(object obj, int attempt)
		{
			try
			{
				await _repository.Save(obj);
			}
			catch (Exception)
			{
				if (attempt >= 3) throw;

				attempt++;

				await SaveInternal(obj, attempt);
			}
		}
	}

	[TestFixture]
	public class ServiceTest
	{
		[Test]
		public void SaveShouldPerform3Attempts()
		{
			//arrange
			int attemptsCounter = 0;

			var repoMock = new Mock<IRepository>();
			repoMock.Setup(x => x.Save(It.IsAny<object>()))
				.Callback(() =>
				{
					attemptsCounter++;
					throw new Exception();
				});

			var sut = new Service(repoMock.Object);

			//act
			//assert
			Assert.ThrowsAsync<Exception>(async () => await sut.Save(null));

			Assert.AreEqual(3, attemptsCounter);
		}
	}
}