using System.Collections.Generic;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Moq;
using Xunit;

namespace ch1seL.Blazored.LocalStorage.Concurrent.UnitTests
{
    public class ConcurrentEntityStorageTests
    {
        [Fact]
        public async Task ContainsGenericTypeNameInKey()
        {
            var localStorageMock = new Mock<ILocalStorageService>();
            var storage = new TestStorage<string>(localStorageMock.Object);
            const string expectedKeyName = "TestStorage<String>";

            await storage.EmptyRequest();

            localStorageMock.Verify(s => s.GetItemAsync<List<string>>(It.Is<string>(k => k == expectedKeyName)));
        }

        private class TestStorage<T> : ConcurrentEntityStorageBase<List<T>>
        {
            public TestStorage(ILocalStorageService localStorageService) : base(localStorageService)
            {
            }

            public async Task EmptyRequest()
            {
                await Request(list => list);
            }
        }
    }
}