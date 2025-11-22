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

            string dominosJson = @"
{
  ""menuID"": ""dominos"",
  ""menuName"": ""Dominos"",
  ""meals"": [
    {
      ""id"": ""dominos-olv"",
      ""name"": ""Olive Pizza"",
      ""imageUrl"": ""assets/dominos_PNG31.png"",
      ""category"": ""dominos"",
      ""price"": 3.25
    },
    {
      ""id"": ""dominos-mini"",
      ""name"": ""Mini Snack Pizza"",
      ""imageUrl"": ""assets/dominos_PNG4.png"",
      ""category"": ""dominos"",
      ""price"": 1.95
    }
  ]
}
";

            string filePath = Path.Combine(menuDirectoryPath, "dominos.json");
            File.WriteAllText(filePath, dominosJson);

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
            Assert.Equal("dominos", menu.MenuID);
            Assert.NotNull(menu.Meals);
            Assert.Equal(2, menu.Meals.Count);
            Assert.Equal("dominos-olv", menu.Meals[0].ID);
        }

        [Fact]
        public async Task GetMenuAsync_WithExistingID_ReturnsMenu() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            FoodStackMenu? menu = await service.GetMenuAsync("dominos");

            Assert.NotNull(menu);
            Assert.Equal("dominos", menu.MenuID);
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
            Assert.Equal("dominos", ids[0]);
        }
    }
}
