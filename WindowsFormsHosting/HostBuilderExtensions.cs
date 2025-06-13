using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WindowsFormsHosting
{
    /// <summary>
    /// IHostBuilder/IHostApplicationBuilder Extensions
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Add WinForms Hosting
        /// </summary>
        /// <typeparam name="TMainForm"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostBuilder AddWinFormsHosting<TMainForm>(
            this IHostBuilder builder
            ) where TMainForm : Form
        {
            return builder
                .ConfigureServices((context, services) =>
                {
                    // --- メインフォームの登録 (Singleton) ---
                    services.AddSingleton<TMainForm>();
                    // WinFormsProviderの登録
                    services.AddSingleton<IWinFormsProvider, WinFormsProvider>();

                    // --- ApplicationContext の登録 (Singleton) ---
                    // 実装型、基底クラス型、インターフェース型で解決できるように登録
                    services.AddSingleton<WinFormsAppContext<TMainForm>>();
                    services.AddSingleton<ApplicationContext>(sp => sp.GetRequiredService<WinFormsAppContext<TMainForm>>());
                    services.AddSingleton<IShutdownRequestHandler>(sp => sp.GetRequiredService<WinFormsAppContext<TMainForm>>());

                    // --- WinFormsHostedService の登録 (Singleton) ---
                    services.AddHostedService<WinFormsHostedService>();
                });
        }

        /// <summary>
        /// Add WinForms Hosting
        /// </summary>
        /// <typeparam name="TMainForm"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostApplicationBuilder AddWinFormsHosting<TMainForm>(
            this IHostApplicationBuilder builder
            ) where TMainForm : Form
        {
            // --- メインフォームの登録 (Singleton) ---
            builder.Services.AddSingleton<TMainForm>();
            // WinFormsProviderの登録
            builder.Services.AddSingleton<IWinFormsProvider, WinFormsProvider>();

            // --- ApplicationContext の登録 (Singleton) ---
            // 実装型、基底クラス型、インターフェース型で解決できるように登録
            builder.Services.AddSingleton<WinFormsAppContext<TMainForm>>();
            builder.Services.AddSingleton<ApplicationContext>(sp => sp.GetRequiredService<WinFormsAppContext<TMainForm>>());
            builder.Services.AddSingleton<IShutdownRequestHandler>(sp => sp.GetRequiredService<WinFormsAppContext<TMainForm>>());

            // --- WinFormsHostedService の登録 (Singleton) ---
            builder.Services.AddHostedService<WinFormsHostedService>();

            return builder;
        }
    }
}
