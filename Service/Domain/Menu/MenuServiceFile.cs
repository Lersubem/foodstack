using System.Text.Json;

namespace FoodStack.Domain.Menu {

    public class MenuServiceFile : IMenuService {
        private readonly string menuDirectoryPath;
        private readonly JsonSerializerOptions jsonOptions;

        public MenuServiceFile(string menuDirectoryPath) {
            if (string.IsNullOrWhiteSpace(menuDirectoryPath)) {
                throw new ArgumentException("menuDirectoryPath is required.", nameof(menuDirectoryPath));
            }

            this.menuDirectoryPath = menuDirectoryPath;
            this.jsonOptions = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IReadOnlyList<FoodStackMenu>> GetAllMenusAsync() {
            try {
                List<FoodStackMenu> menus = new List<FoodStackMenu>();

                if (Directory.Exists(this.menuDirectoryPath) == false) {
                    return menus;
                }

                string[] files = Directory.GetFiles(this.menuDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in files) {
                    FoodStackMenu? menu = await this.LoadMenuFromFileAsync(file);

                    if (menu != null) {
                        menus.Add(menu);
                    }
                }

                return menus;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        public async Task<FoodStackMenu?> GetMenuAsync(string menuID) {
            try {
                if (string.IsNullOrWhiteSpace(menuID)) {
                    throw new ArgumentException("menuID is required.", nameof(menuID));
                }

                string fileName = menuID + ".json";
                string filePath = Path.Combine(this.menuDirectoryPath, fileName);

                if (File.Exists(filePath) == false) {
                    return null;
                }

                FoodStackMenu? menu = await this.LoadMenuFromFileAsync(filePath);

                return menu;
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        public Task<IReadOnlyList<string>> GetMenuIDsAsync() {
            try {
                List<string> ids = new List<string>();

                if (Directory.Exists(this.menuDirectoryPath) == false) {
                    return Task.FromResult<IReadOnlyList<string>>(ids);
                }

                string[] files = Directory.GetFiles(this.menuDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);

                foreach (string file in files) {
                    string id = Path.GetFileNameWithoutExtension(file);

                    if (string.IsNullOrWhiteSpace(id) == false) {
                        ids.Add(id);
                    }
                }

                return Task.FromResult<IReadOnlyList<string>>(ids);
            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }

        private async Task<FoodStackMenu?> LoadMenuFromFileAsync(string filePath) {
            try {
                string json = await File.ReadAllTextAsync(filePath);
                FoodStackMenu? menu = JsonSerializer.Deserialize<FoodStackMenu>(json, this.jsonOptions);

                if (menu == null) {
                    return null;
                } else {
                    return menu;
                }

            } catch (Exception exception) {
                Console.WriteLine(exception.ToString());
                throw;
            }
        }
    }
}
