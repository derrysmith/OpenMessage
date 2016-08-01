﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenMessage.Providers.Azure.Configuration;
using OpenMessage.Providers.Azure.Conventions;
using OpenMessage.Providers.Azure.Management;
using OpenMessage.Providers.Azure.Serialization;
using System;

namespace OpenMessage.Providers.Azure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddOpenMessage(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddScoped<IQueueNamingConvention, DefaultNamingConventions>();
            services.TryAddScoped<ISubscriptionNamingConvention, DefaultNamingConventions>();
            services.TryAddScoped<ITopicNamingConvention, DefaultNamingConventions>();

            return services.AddScoped<ISerializationProvider, SerializationProvider>();
        }

        public static IServiceCollection AddQueueObservable<T>(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return services.AddBaseServices<T>().AddQueue<T>();
        }
        public static IServiceCollection AddQueueObservable<T>(this IServiceCollection services, Action<T> callback)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return services.AddBaseServices<T>().AddQueue<T>().AddObserver(callback);
        }

        public static IServiceCollection AddSubscriptionObservable<T>(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return services.AddBaseServices<T>().AddSubscription<T>();
        }
        public static IServiceCollection AddSubscriptionObservable<T>(this IServiceCollection services, Action<T> callback)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return services.AddBaseServices<T>().AddSubscription<T>().AddObserver(callback);
        }

        public static IServiceCollection AddQueueDispatcher<T>(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return services.AddQueue<T>();
        }

        public static IServiceCollection AddTopicDispatcher<T>(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return services.AddTopic<T>();
        }

        private static IServiceCollection AddQueue<T>(this IServiceCollection services)
        {
            return services.AddScoped<IQueueFactory<T>, QueueFactory<T>>();
        }
        private static IServiceCollection AddTopic<T>(this IServiceCollection services)
        {
            return services.AddScoped<ITopicFactory<T>, TopicFactory<T>>();
        }
        private static IServiceCollection AddSubscription<T>(this IServiceCollection services)
        {
            return services.AddTopic<T>().AddScoped<ISubscriptionFactory<T>, SubscriptionFactory<T>>();
        }
        private static IServiceCollection AddBroker<T>(this IServiceCollection services)
        {
            services.TryAddScoped<IBroker<T>, MessageBroker<T>>();

            return services;
        }
        private static IServiceCollection AddBaseServices<T>(this IServiceCollection services)
        {
            services.TryAddScoped<IConfigureOptions<OpenMessageAzureProviderOptions<T>>, OpenMessageAzureProviderOptionsConfigurator<T>>();

            return services.AddScoped<INamespaceManager<T>, NamespaceManager<T>>();
        }
    }
}