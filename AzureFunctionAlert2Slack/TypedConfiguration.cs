﻿using AzureMonitorAlertToSlack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace AzureFunctionAlert2Slack
{
    public class TypedConfiguration
    {
        public static T ConfigureTypedConfiguration<T>(IServiceCollection services, IConfiguration config, string sectionName, bool throwOnMissing = true)
            where T : new()
        {
            // TODO: (low) continue investigation - how to avoid reflection and get validation errors immediately

            // Note: This does NOT cause validation on startup...: services.AddOptions<AceKnowledgeConfiguration>().Bind(config.GetSection("AceKnowledge")).ValidateDataAnnotations().ValidateOnStart();

            // https://referbruv.com/blog/posts/working-with-options-pattern-in-aspnet-core-the-complete-guide
            var appSettings = new T();

            config.GetSection(sectionName).Bind(appSettings);
            services.AddSingleton(appSettings.GetType(), appSettings!);

            var props = appSettings.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(o => !o.PropertyType.IsSealed); // TODO: (low) better check than IsSealed (also unit test)
            foreach (var prop in props)
            {
                var instance = prop.GetValue(appSettings);
                if (instance == null)
                {
                    if (throwOnMissing)
                        throw new NullReferenceException($"{prop.Name} does not exist");
                }
                else
                {
                    //config.GetSection(prop.Name).Bind(instance);
                    services.AddSingleton(instance.GetType(), instance!);

                    //var asOptions = Microsoft.Extensions.Options.Options.Create(instance);
                    //services.ConfigureOptions(instance);

                    // Execute validation (if available)
                    var validatorType = instance.GetType().Assembly.GetTypes()
                       .Where(t =>
                       {
                           var validatorInterface = t.GetInterfaces().SingleOrDefault(o =>
                           o.IsGenericType && o.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Options.IValidateOptions<>));
                           return validatorInterface != null && validatorInterface.GenericTypeArguments.Single() == instance.GetType();
                       }).FirstOrDefault();

                    if (validatorType != null)
                    {
                        var validator = Activator.CreateInstance(validatorType);
                        var m = validatorType.GetMethod("Validate");
                        var result = (Microsoft.Extensions.Options.ValidateOptionsResult?)m?.Invoke(validator, new object[] { "", instance });
                        if (result!.Failed)
                        {
                            throw new Exception($"{validatorType.Name}: {result.FailureMessage}");
                        }
                    }
                }
            }

            return appSettings;
            // https://kaylumah.nl/2021/11/29/validated-strongly-typed-ioptions.html
            // If we want to inject IOptions<Type> instead of just Type, this is needed: https://stackoverflow.com/a/61157181 services.ConfigureOptions(instance)
            //services.Configure<AceKnowledgeOptions>(config.GetSection("AceKnowledge"));
        }
        //static void ConfigureAppConfiguration(IConfigurationBuilder configBuilder, IHostEnvironment env)
        //{
        //    // CreateDefaultBuilder messes up providers, doing it manually: https://github.com/dotnet/aspnetcore/issues/19924
        //    configBuilder.Sources.Clear();
        //    configBuilder.SetBasePath(Directory.GetCurrentDirectory());
        //    configBuilder.AddJsonFile("appsettings.json");
        //    configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);
        //    configBuilder.AddUserSecrets<Program>();
        //    configBuilder.AddEnvironmentVariables();
        //}
    }

}