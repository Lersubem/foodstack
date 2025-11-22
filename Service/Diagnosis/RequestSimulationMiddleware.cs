using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace FoodStack.Diagnosis {
    public sealed class RequestSimulationMiddleware {
        private readonly RequestDelegate _next;
        private static readonly TimeSpan MaxQueueDelay = TimeSpan.FromMinutes(0.5);

        public RequestSimulationMiddleware(RequestDelegate next) {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                if (context == null) {
                    throw new ArgumentNullException(nameof(context));
                }

                if (IsSimulatedEndpoint(context) == false) {
                    await this._next(context);
                    return;
                }

                int choice = Random.Shared.Next(0, 100);

                if (choice <= 35) {
                    await this.SimulateQueueAsync(context);
                    return;
                }

                if (choice <= 70) {
                    await this.SimulatePartialResponseAndAbortAsync(context);
                    return;
                }

                await this._next(context);
            } catch (OperationCanceledException) {
                Log.Warning("RequestSimulationMiddleware canceled for {Path}", context.Request.Path);
            } catch (Exception ex) {
                Log.Error(ex, "Error in RequestSimulationMiddleware for {Path}", context.Request.Path);
                throw;
            }
        }
        private static bool IsSimulatedEndpoint(HttpContext context) {
            string method = context.Request.Method;

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) == false) {
                return false;
            }

            PathString path = context.Request.Path;

            if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) == false) {
                return false;
            }

            return true;
        }


        private async Task SimulateQueueAsync(HttpContext context) {
            try {
                int maxMilliseconds = (int)MaxQueueDelay.TotalMilliseconds;
                int delayMilliseconds = Random.Shared.Next(maxMilliseconds / 2, maxMilliseconds);

                Log.Information("Simulating queued request for {Delay} ms on {Path}", delayMilliseconds, context.Request.Path);

                await Task.Delay(delayMilliseconds, context.RequestAborted);
                await this._next(context);
            } catch (Exception ex) {
                Log.Error(ex, "Error in SimulateQueueAsync for {Path}", context.Request.Path);
                throw;
            }
        }

        private async Task SimulatePartialResponseAndAbortAsync(HttpContext context) {
            try {
                Log.Information("Simulating partial response and abort on {Path}", context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json; charset=utf-8";

                string payload = "{\"status\":\"partial\",\"message\":\"Simulated dropped connection\"";
                byte[] data = Encoding.UTF8.GetBytes(payload);

                await context.Response.Body.WriteAsync(data, 0, data.Length, context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);

                context.Abort();
            } catch (Exception ex) {
                Log.Error(ex, "Error in SimulatePartialResponseAndAbortAsync for {Path}", context.Request.Path);
                throw;
            }
        }
    }
}
