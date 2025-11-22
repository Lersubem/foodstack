namespace FoodStack.Domain.Menu {
    public interface IMenuService {
        Task<IReadOnlyList<FoodStackMenu>> GetAllMenusAsync();
        Task<FoodStackMenu?> GetMenuAsync(string menuID);
        Task<IReadOnlyList<string>> GetMenuIDsAsync();
    }
}
