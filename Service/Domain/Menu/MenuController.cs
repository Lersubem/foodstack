using Microsoft.AspNetCore.Mvc;

namespace FoodStack.Domain.Menu {
    /// <summary>
    /// Provides endpoints for reading food menus.
    /// </summary>
    [ApiController]
    [Route("api/menu")]
    [Produces("application/json")]
    public class MenuController : ControllerBase {
        private readonly IMenuService menuService;
        private readonly ILogger<MenuController> logger;

        public MenuController(IMenuService menuService, ILogger<MenuController> logger) {
            this.menuService = menuService;
            this.logger = logger;
        }

        /// <summary>
        /// Gets all available menus.
        /// </summary>
        /// <returns>List of menus with their meals.</returns>
        /// <response code="200">Returns all menus.</response>
        /// <response code="500">Unexpected error.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<FoodStackMenu>>> GetMenus() {
            try {
                IReadOnlyList<FoodStackMenu> menus = await this.menuService.GetAllMenusAsync();
                return this.Ok(menus);
            } catch (Exception exception) {
                this.logger.LogError(exception, "Error while getting all menus.");
                throw;
            }
        }

        /// <summary>
        /// Gets a single menu by its ID.
        /// </summary>
        /// <param name="menuID">The menu identifier (e.g. file name without extension).</param>
        /// <returns>The requested menu with its meals.</returns>
        /// <response code="200">Returns the requested menu.</response>
        /// <response code="400">menuID is missing or invalid.</response>
        /// <response code="404">Menu not found.</response>
        /// <response code="500">Unexpected error.</response>
        [HttpGet("{menuID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FoodStackMenu>> GetMenu(string menuID) {
            try {
                if (string.IsNullOrWhiteSpace(menuID)) {
                    return this.BadRequest("menuID is required.");
                }

                FoodStackMenu? menu = await this.menuService.GetMenuAsync(menuID);

                if (menu == null) {
                    return this.NotFound();
                }

                return this.Ok(menu);
            } catch (Exception exception) {
                this.logger.LogError(exception, "Error while getting menu {MenuID}.", menuID);
                throw;
            }
        }

        /// <summary>
        /// Gets all available menu IDs.
        /// </summary>
        /// <returns>List of menu IDs.</returns>
        /// <response code="200">Returns all menu IDs.</response>
        /// <response code="500">Unexpected error.</response>
        [HttpGet("ids")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<string>>> GetMenuIDs() {
            try {
                IReadOnlyList<string> ids = await this.menuService.GetMenuIDsAsync();
                return this.Ok(ids);
            } catch (Exception exception) {
                this.logger.LogError(exception, "Error while getting menu IDs.");
                throw;
            }
        }
    }
}
