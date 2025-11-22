using FoodStack.Domain.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodStack.Test {
    public class MenuServiceFileTests {
        private MenuServiceFile CreateServiceWithSingleMenu(out string menuDirectoryPath) {
            menuDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "FoodStackTests",
                "Menu",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(menuDirectoryPath);

            string baguetteJson = @"
[
  {
    ""id"": ""baguette-16"",
    ""name"": ""Classic French Baguette"",
    ""imageUrl"": ""/img/baguette-16.png"",
    ""category"": ""baguette"",
    ""price"": 3.50
  },
  {
    ""id"": ""baguette-20"",
    ""name"": ""Sesame Seed Baguette"",
    ""imageUrl"": ""/img/baguette-20.png"",
    ""category"": ""baguette"",
    ""price"": 4.00
  }
]";

            string filePath = Path.Combine(menuDirectoryPath, "baguette.json");
            File.WriteAllText(filePath, baguetteJson);

            MenuServiceFile service = new MenuServiceFile(menuDirectoryPath);

            return service;
        }

        [Fact]
        public async Task GetAllMenusAsync_ReturnsMenusWithMeals() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            IReadOnlyList<FoodStackMenu> menus = await service.GetAllMenusAsync();

            Assert.NotNull(menus);
            Assert.Single(menus);

            FoodStackMenu menu = menus[0];
            Assert.Equal("baguette", menu.MenuID);
            Assert.NotNull(menu.Meals);
            Assert.Equal(2, menu.Meals.Count);
            Assert.Equal("baguette-16", menu.Meals[0].ID);
        }

        [Fact]
        public async Task GetMenuAsync_WithExistingID_ReturnsMenu() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            FoodStackMenu? menu = await service.GetMenuAsync("baguette");

            Assert.NotNull(menu);
            Assert.Equal("baguette", menu!.MenuID);
            Assert.Equal(2, menu.Meals.Count);
        }

        [Fact]
        public async Task GetMenuAsync_WithNonExistingID_ReturnsNull() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            FoodStackMenu? menu = await service.GetMenuAsync("does-not-exist");

            Assert.Null(menu);
        }

        [Fact]
        public async Task GetMenuIDsAsync_ReturnsFileBasedIDs() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            IReadOnlyList<string> ids = await service.GetMenuIDsAsync();

            Assert.NotNull(ids);
            Assert.Single(ids);
            Assert.Equal("baguette", ids[0]);
        }
    }
}
