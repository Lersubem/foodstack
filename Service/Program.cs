using FoodStack.Diagnosis;
using FoodStack.Domain.Menu;
using FoodStack.Domain.Order;
using Serilog;
using Serilog.Sinks.Map;
using System.IO;

namespace FoodStack {
    public class Program {
        public static void Main(string[] args) {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            const string CorsPolicyName = "FrontendCors";

            builder.Services.AddCors(options => {
                options.AddPolicy(CorsPolicyName, policy => {
                    policy
                          .AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            ConfigureLogging(builder);
            ConfigureServices(builder.Services);

            WebApplication app = builder.Build();

            ConfigureApplication(app);

            try {
                app.Run();
            } finally {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureLogging(WebApplicationBuilder builder) {
            string rootFolder = builder.Configuration.GetValue<string>("FileLogging:RootFolder", "Logs");
            long maxBytes = builder.Configuration.GetValue<long>("FileLogging:MaxFileBytes", 5 * 1024 * 1024);
            int retained = builder.Configuration.GetValue<int>("FileLogging:RetainedFileCountLimit", 31);

            builder.Logging.ClearProviders();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Map(
                    keySelector: logEvent => logEvent.Timestamp.ToString("yyyy-MM-dd"),
                    configure: (date, writeTo) => {
                        string path = Path.Combine(rootFolder, date, "0000.log");

                        writeTo.File(
                            path: path,
                            rollingInterval: RollingInterval.Infinite,
                            fileSizeLimitBytes: maxBytes,
                            rollOnFileSizeLimit: true,
                            retainedFileCountLimit: retained,
                            shared: true);
                    })
                .CreateLogger();

            builder.Host.UseSerilog();
        }

        private static void ConfigureServices(IServiceCollection services) {
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options => {
                options.SchemaFilter<OrderPlacementResultSchemaFilter>();
            });

            string menuDirectoryPath = Path.Combine(AppContext.BaseDirectory, "Data", "Menu");
            string ordersDirectoryPath = Path.Combine(AppContext.BaseDirectory, "Data", "Orders");

            services.AddSingleton<IMenuService>(serviceProvider => {
                return new MenuServiceFile(menuDirectoryPath);
            });

            services.AddSingleton<IOrderService>(serviceProvider => {
                IMenuService menuService = serviceProvider.GetRequiredService<IMenuService>();
                return new OrderServiceFile(ordersDirectoryPath, menuService);
            });
        }

        private static void ConfigureApplication(WebApplication app) {
            if (app.Environment.IsDevelopment() != true) {
                app.UsePathBase("/foodstack/service");
            }
            app.UseExceptionHandling();

            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();
            app.UseCors("FrontendCors");
            app.UseMiddleware<RequestSimulationMiddleware>();

            //app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}
