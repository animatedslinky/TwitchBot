﻿using System;

using System.Windows;

using System.Windows.Data;

using System.Windows.Media;
using System.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Slinkybot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isRunning;
        private ConnectionConfig connectionConfig;

        private string connectionSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlinkyBot\\ConnectionSettings.xml");

        private TwitchChat chatBot;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private CollectionView view;
        public MainWindow()
        {
            isRunning = false;
            bool configured = false;

            connectionConfig = new ConnectionConfig();
            do
            {
                if (File.Exists(connectionSettingsFile))
                {
                    try
                    {
                        using (StreamReader file = File.OpenText(connectionSettingsFile))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            connectionConfig = (ConnectionConfig)serializer.Deserialize(file, typeof(ConnectionConfig));
                            configured = true;
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Something is configured incorrectly");                        
                    }
                }
                if (!configured)
                {
                    configured = Configure(connectionConfig);
                    if (!configured)
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                }
            } while (!configured);

            InitializeComponent();

            chatBot = new TwitchChat(connectionConfig);

            lvUsers.ItemsSource = chatBot.gymBotCommand.gymLeaders;

            view = (CollectionView)CollectionViewSource.GetDefaultView(lvUsers.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("gymType");
            view.GroupDescriptions.Add(groupDescription);
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private bool Configure(ConnectionConfig config)
        {
            ConfigureDialog dialog = new ConfigureDialog(config);
            bool ret = false;
            if (dialog.ShowDialog() == true)
            {
                connectionConfig = new ConnectionConfig();
                connectionConfig.username = dialog.UserName;
                connectionConfig.oauth = dialog.OAuth;
                connectionConfig.channel = dialog.Channel;
                using (StreamWriter file = File.CreateText(connectionSettingsFile))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, connectionConfig);
                }
                ret = true;
            }
            return ret;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Forcing the CommandManager to raise the RequerySuggested event            
            view.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(connectionConfig.username) || string.IsNullOrEmpty(connectionConfig.oauth) || string.IsNullOrEmpty(connectionConfig.channel))
            {
                MessageBox.Show("Something is not configured");
            }
            else
            {
                isRunning = !isRunning;
                if (isRunning)
                {
                    botStateText.Text = "Bot is currently Enabled";
                    botStateText.Foreground = new SolidColorBrush(Colors.Green);
                    WTBotButton.IsEnabled = true;
                    
                    chatBot.Connect();
                }
                else
                {
                    botStateText.Text = "Bot is currently Disabled";
                    botStateText.Foreground = new SolidColorBrush(Colors.Red);
                    WTBotButton.IsEnabled = false;
                    chatBot.Disconnect();
                }
            }
        }

        private void WTButton_Click(object sender, RoutedEventArgs e)
        {
            chatBot.runWondertrade();
        }

        private void ConfigureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Configure(connectionConfig))
            {
                MessageBox.Show("If you updated your configuration you must restart the app.  Sorry.");
            }
        }

        private void AddLeaderMenu_Click(object sender, RoutedEventArgs e)
        {
            AddUpdateDialog dialog = new AddUpdateDialog();
            if (dialog.ShowDialog() == true)
            {
                chatBot.gymBotCommand.AddOrUpdateLeader(dialog.leader);
            }
        }

        private void UpdateLeaderMenu_Click(object sender, RoutedEventArgs e)
        {
            SelectLeaderDialog selectLeaderDialog = new SelectLeaderDialog(chatBot.gymBotCommand.gymLeaders,"Modify gym information for");
            if (selectLeaderDialog.ShowDialog() == true)
            {
                var leader = from l in chatBot.gymBotCommand.gymLeaders
                                   where l.Name.ToLower() == selectLeaderDialog.SelectedLeader.ToLower()
                                   select l;
                AddUpdateDialog dialog = new AddUpdateDialog(leader.FirstOrDefault());
                if (dialog.ShowDialog() == true)
                {
                    chatBot.gymBotCommand.AddOrUpdateLeader(dialog.leader);
                }
            }
        }

        private void RemoveLeader_Click(object sender, RoutedEventArgs e)
        {
            SelectLeaderDialog selectLeaderDialog = new SelectLeaderDialog(chatBot.gymBotCommand.gymLeaders, "DELETE GYM LEADER");
            if (selectLeaderDialog.ShowDialog() == true)
            {
                chatBot.gymBotCommand.RemoveLeader(selectLeaderDialog.SelectedLeader);
            }
        }
    }
}
