using System.Text.Json;

namespace FoodStack.Diagnosis {
    public class ExceptionHandlingMiddleware {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionHandlingMiddleware> logger;
        private readonly IHostEnvironment environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment) {
            this.next = next;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task Invoke(HttpContext context) {
            try {
                await this.next(context);
            } catch (Exception exception) {
                try {
                    this.logger.LogError(exception, "Unhandled exception while processing request.");

                    if (context.Response.HasStarted == false) {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                        if (this.environment.IsDevelopment()) {
                            context.Response.ContentType = "text/plain";
                            await context.Response.WriteAsync(exception.ToString());
                        } else {
                            context.Response.ContentType = "application/json";

                            object payload = new {
                                status = StatusCodes.Status500InternalServerError,
                                error = "InternalServerError",
                                message = "An unexpected error occurred."
                            };

                            string json = JsonSerializer.Serialize(payload);
                            await context.Response.WriteAsync(json);
                        }
                    }
                } catch {
                }
            }
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}
