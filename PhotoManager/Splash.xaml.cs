
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using PhotoManager;
using WHLClasses;
using WHLClasses.Authentication;

namespace PhotoManager
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class Splash : Window
    {
        private BackgroundWorker Worker = new BackgroundWorker();
        internal MainWindow HomeRef = null;
        internal void SplashLoad()
        {
            Worker.DoWork += SplashProxy;
            Worker.ProgressChanged += ProxyTextUpdate;
            Worker.RunWorkerCompleted += ProxyFinished;
            Worker.WorkerReportsProgress = true;
            Worker.RunWorkerAsync();
        }

        private void LoadAssemblies(Assembly Assembly)
        {
            foreach (AssemblyName name in Assembly.GetReferencedAssemblies())
            {
                Worker.ReportProgress(0, "Loading " + name.Name.ToString());
                Thread.Sleep(25);
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName == name.FullName))
                {
                    LoadAssemblies(Assembly.Load(name));
                    Console.WriteLine(name.FullName);
                }

            }
        }

        internal void SplashProxy(object sender, DoWorkEventArgs e)
        {
            Worker.ReportProgress(0, "Loading Assemblies");
            LoadAssemblies(this.GetType().Assembly);
            GenericDataController loader = new GenericDataController();
            Worker.ReportProgress(0, "Loading Item Data");
            HomeRef.Data_Skus = loader.SmartSkuCollLoad();
            Worker.ReportProgress(0,"Making Mixdown");
            HomeRef.Data_SkusMixdown = HomeRef.Data_Skus.MakeMixdown();
            Worker.ReportProgress(0, "Loading Employee Data");
            HomeRef.Data_Employees = new EmployeeCollection();
            Thread.Sleep(20);
            Worker.ReportProgress(0, "Preparing Authentication");
            HomeRef.Data_User = new AuthClass();


        }

        private void UpdateText(string NewText)
        {
            LoadingStatsText.Text += Environment.NewLine + NewText;
        }

        internal void ProxyTextUpdate(object sender, ProgressChangedEventArgs e)
        {
            UpdateText(e.UserState.ToString());
        }

        internal void ProxyFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateText("Requesting User Login");
            try
            {
                UpdateText("Logged in: " + HomeRef.Data_User.AuthenticatedUser.FullName);
            }
            catch (NullReferenceException)
            {
                HomeRef.Data_User = new AuthClass();
                UpdateText("Logged in: " + HomeRef.Data_User.AuthenticatedUser.FullName);
            }
            finally
            {
                
                Close();
            }
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SplashLoad();
        }
    }
}
