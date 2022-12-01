using AzureMonitorAlertToSlack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace AzureFunctionAlert2Slack
{
    public class TypedConfiguration
    {
        public static T ConfigureTypedConfiguration<T>(IServiceCollection services, IConfiguration config, string sectionName, bool throwOnMissing = true)
            where T : new()
        {
            // TODO: continue investigation - how to avoid reflection and get validation errors immediately
            // Note: The following does NOT cause validation on startup...: services.AddOptions<AceKnowledgeConfiguration>().Bind(config.GetSection("AceKnowledge")).ValidateDataAnnotations().ValidateOnStart();

            // https://referbruv.com/blog/posts/working-with-options-pattern-in-aspnet-core-the-complete-guide
            var appSettings = new T();

            config.GetSection(sectionName).Bind(appSettings);
            // TODO: we should recursively check for missing sections/values (where the property is not nullable)
            //services.AddSingleton(appSettings.GetType(), appSettings!);

            RecurseRegister(new { Root = appSettings }, services);

            return appSettings;
            // https://kaylumah.nl/2021/11/29/validated-strongly-typed-ioptions.html
            // If we want to inject IOptions<Type> instead of just Type, this is needed: https://stackoverflow.com/a/61157181 services.ConfigureOptions(instance)
        }

        private static void RecurseRegister(object obj, IServiceCollection services)
        {
            var props = obj.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(o => !o.PropertyType.IsSealed) // TODO: (low) better check than IsSealed (also unit test)
                .Where(o => o.PropertyType.IsClass && o.PropertyType != typeof(string));

            foreach (var prop in props)
            {
                var instance = prop.GetValue(obj);
                if (instance == null)
                {
                    var nullabilityInfo = new NullabilityInfoContext().Create(prop);
                    if (nullabilityInfo.WriteState is not NullabilityState.Nullable)
                        throw new NullReferenceException($"{nameof(prop.Name)} is null"); // should not be possible
                }
                else
                {
                    // Validate(instance);
                    services.AddSingleton(prop.PropertyType, instance);
                    RecurseRegister(instance, services);
                }
            }
        }

        private static void Validate(object instance)
        {
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

}
