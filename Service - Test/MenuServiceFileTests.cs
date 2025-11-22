using FoodStack.Domain.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FoodStack.Test {
    /// <summary>
    /// Unit tests for <see cref="MenuServiceFile"/> using temporary JSON files as a menu store.
    /// </summary>
    public class MenuServiceFileTests {
        /// <summary>
        /// Creates a temporary directory with a single valid menu JSON file (Dominos) and
        /// returns a <see cref="MenuServiceFile"/> instance bound to that directory.
        /// </summary>
        /// <param name="menuDirectoryPath">
        /// Output parameter containing the path to the temporary directory holding the menu files.
        /// </param>
        /// <returns>A <see cref="MenuServiceFile"/> instance pointing at the created directory.</returns>
        private MenuServiceFile CreateServiceWithSingleMenu(out string menuDirectoryPath) {
            menuDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "FoodStackTests",
                "Menu",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(menuDirectoryPath);

            // Single valid Dominos menu with two meals.
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

        /// <summary>
        /// Creates a temporary directory with no JSON files and returns a service bound to it.
        /// Used to validate behavior when no menus exist.
        /// </summary>
        /// <param name="menuDirectoryPath">Path to the empty temporary directory.</param>
        /// <returns>A <see cref="MenuServiceFile"/> instance pointing at an empty directory.</returns>
        private MenuServiceFile CreateServiceWithEmptyDirectory(out string menuDirectoryPath) {
            menuDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "FoodStackTests",
                "MenuEmpty",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(menuDirectoryPath);

            MenuServiceFile service = new MenuServiceFile(menuDirectoryPath);

            return service;
        }

        /// <summary>
        /// Creates a temporary directory with two valid menu files (Dominos and KFC) to validate
        /// behavior when multiple menus are present.
        /// </summary>
        /// <param name="menuDirectoryPath">Path to the temporary directory with multiple menus.</param>
        /// <returns>A <see cref="MenuServiceFile"/> instance pointing at the directory.</returns>
        private MenuServiceFile CreateServiceWithMultipleMenus(out string menuDirectoryPath) {
            menuDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "FoodStackTests",
                "MenuMultiple",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(menuDirectoryPath);

            // Valid Dominos menu JSON.
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
    }
  ]
}
";

            // Valid KFC menu JSON.
            string kfcJson = @"
{
  ""menuID"": ""kfc"",
  ""menuName"": ""KFC"",
  ""meals"": [
    {
      ""id"": ""kfc-bucket"",
      ""name"": ""Family Bucket"",
      ""imageUrl"": ""assets/kfc_bucket.png"",
      ""category"": ""kfc"",
      ""price"": 9.99
    }
  ]
}
";

            File.WriteAllText(Path.Combine(menuDirectoryPath, "dominos.json"), dominosJson);
            File.WriteAllText(Path.Combine(menuDirectoryPath, "kfc.json"), kfcJson);

            MenuServiceFile service = new MenuServiceFile(menuDirectoryPath);

            return service;
        }

        /// <summary>
        /// Creates a temporary directory with one valid menu file and one malformed JSON file,
        /// plus a non-JSON text file, to verify robustness against invalid files.
        /// </summary>
        /// <param name="menuDirectoryPath">Path to the temporary directory with mixed files.</param>
        /// <returns>A <see cref="MenuServiceFile"/> instance pointing at the directory.</returns>
        private MenuServiceFile CreateServiceWithMalformedMenuFile(out string menuDirectoryPath) {
            menuDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "FoodStackTests",
                "MenuMalformed",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(menuDirectoryPath);

            // Valid Dominos menu JSON.
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
    }
  ]
}
";

            // Intentionally malformed JSON (missing closing braces and array).
            string malformedJson = @"{ ""menuID"": ""broken"", ""menuName"": ""Broken"" ";

            File.WriteAllText(Path.Combine(menuDirectoryPath, "dominos.json"), dominosJson);
            File.WriteAllText(Path.Combine(menuDirectoryPath, "broken.json"), malformedJson);

            // Non-JSON file that should be ignored by the service.
            File.WriteAllText(Path.Combine(menuDirectoryPath, "readme.txt"), "this is not json");

            MenuServiceFile service = new MenuServiceFile(menuDirectoryPath);

            return service;
        }

        /// <summary>
        /// Ensures <see cref="MenuServiceFile.GetAllMenusAsync"/> returns a single menu with its meals
        /// when exactly one valid JSON menu file is present.
        /// </summary>
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

        /// <summary>
        /// Ensures <see cref="MenuServiceFile.GetMenuAsync(string)"/> returns the expected menu
        /// when called with an existing menu ID.
        /// </summary>
        [Fact]
        public async Task GetMenuAsync_WithExistingID_ReturnsMenu() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            FoodStackMenu? menu = await service.GetMenuAsync("dominos");

            Assert.NotNull(menu);
            Assert.Equal("dominos", menu.MenuID);
            Assert.Equal(2, menu.Meals.Count);
        }

        /// <summary>
        /// Ensures <see cref="MenuServiceFile.GetMenuAsync(string)"/> returns null
        /// when called with a non-existing menu ID.
        /// </summary>
        [Fact]
        public async Task GetMenuAsync_WithNonExistingID_ReturnsNull() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            FoodStackMenu? menu = await service.GetMenuAsync("does-not-exist");

            Assert.Null(menu);
        }

        /// <summary>
        /// Ensures <see cref="MenuServiceFile.GetMenuIDsAsync"/> returns the menu IDs
        /// derived from the existing JSON files.
        /// </summary>
        [Fact]
        public async Task GetMenuIDsAsync_ReturnsFileBasedIDs() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            IReadOnlyList<string> ids = await service.GetMenuIDsAsync();

            Assert.NotNull(ids);
            Assert.Single(ids);
            Assert.Equal("dominos", ids[0]);
        }

        /// <summary>
        /// Verifies that <see cref="MenuServiceFile.GetAllMenusAsync"/> returns an empty collection
        /// when the menu directory exists but contains no JSON files.
        /// </summary>
        [Fact]
        public async Task GetAllMenusAsync_WithEmptyDirectory_ReturnsEmptyList() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithEmptyDirectory(out menuDirectoryPath);

            IReadOnlyList<FoodStackMenu> menus = await service.GetAllMenusAsync();

            Assert.NotNull(menus);
            Assert.Empty(menus);
        }

        /// <summary>
        /// Verifies that <see cref="MenuServiceFile.GetMenuIDsAsync"/> returns an empty collection
        /// when the menu directory exists but contains no JSON files.
        /// </summary>
        [Fact]
        public async Task GetMenuIDsAsync_WithEmptyDirectory_ReturnsEmptyList() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithEmptyDirectory(out menuDirectoryPath);

            IReadOnlyList<string> ids = await service.GetMenuIDsAsync();

            Assert.NotNull(ids);
            Assert.Empty(ids);
        }

        /// <summary>
        /// Verifies that <see cref="MenuServiceFile.GetAllMenusAsync"/> returns all menus
        /// when multiple valid JSON menu files exist.
        /// </summary>
        [Fact]
        public async Task GetAllMenusAsync_WithMultipleMenus_ReturnsAllMenus() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithMultipleMenus(out menuDirectoryPath);

            IReadOnlyList<FoodStackMenu> menus = await service.GetAllMenusAsync();

            Assert.NotNull(menus);
            Assert.Equal(2, menus.Count);

            FoodStackMenu? dominos = menus.FirstOrDefault(x => x.MenuID == "dominos");
            FoodStackMenu? kfc = menus.FirstOrDefault(x => x.MenuID == "kfc");

            Assert.NotNull(dominos);
            Assert.NotNull(kfc);
            Assert.Single(dominos!.Meals);
            Assert.Single(kfc!.Meals);
        }

        /// <summary>
        /// Verifies that <see cref="MenuServiceFile.GetMenuIDsAsync"/> returns all menu IDs
        /// when multiple valid JSON menu files exist.
        /// </summary>
        [Fact]
        public async Task GetMenuIDsAsync_WithMultipleMenus_ReturnsAllIDs() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithMultipleMenus(out menuDirectoryPath);

            IReadOnlyList<string> ids = await service.GetMenuIDsAsync();

            Assert.NotNull(ids);
            Assert.Equal(2, ids.Count);
            Assert.Contains("dominos", ids);
            Assert.Contains("kfc", ids);
        }

        /// <summary>
        /// Verifies that whitespace-only menu IDs are rejected and an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        [Fact]
        public async Task GetMenuAsync_WithWhitespaceID_ThrowsArgumentException() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithSingleMenu(out menuDirectoryPath);

            Task act = service.GetMenuAsync("  ");

            await Assert.ThrowsAsync<ArgumentException>(async () => await act);
        }

        /// <summary>
        /// Verifies that malformed JSON causes a <see cref="JsonException"/> so callers can handle it explicitly.
        /// </summary>
        [Fact]
        public async Task GetAllMenusAsync_WithMalformedJsonFile_ThrowsJsonException() {
            string menuDirectoryPath;
            MenuServiceFile service = this.CreateServiceWithMalformedMenuFile(out menuDirectoryPath);

            Task act = service.GetAllMenusAsync();

            await Assert.ThrowsAsync<JsonException>(async () => await act);
        }
    }
}
