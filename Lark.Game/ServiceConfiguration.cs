using Microsoft.Extensions.DependencyInjection;

public static class ServiceConfiguration {
  public static IServiceCollection AddGame(this IServiceCollection services) {
    services.AddHostedService<Game>();
    return services;
  }
}