using Autofac;
using AlterEgo.Core.Repositories;
using AlterEgo.Core.Services;
using AlterEgo.Infrastructure.Repositories;
using AlterEgo.Infrastructure.Services;

namespace AlterEgo.Infrastructure.Autofac;

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UsersRepository>()
            .As<IUsersRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<RefreshTokensRepository>()
            .As<IRefreshTokensRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PasswordHasher>()
            .As<IPasswordHasher>()
            .SingleInstance();

        builder.RegisterType<JwtService>()
            .As<IJwtService>()
            .SingleInstance();

        builder.RegisterType<LlmService>()
            .As<ILlmService>()
            .SingleInstance();
    }
}
