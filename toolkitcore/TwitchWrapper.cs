﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ToolkitCore.Controllers;
using ToolkitCore.Models;
using ToolkitCore.Utilities;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Interfaces;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using UnityEngine;
using Verse;

namespace ToolkitCore
{
    public static class TwitchWrapper
    {
        public static TwitchClient Client { get; private set; }

        public static void StartAsync()
        {
            Initialize(new ConnectionCredentials(ToolkitCoreSettings.bot_username, ToolkitCoreSettings.oauth_token));
        }

        public static void Initialize(ConnectionCredentials credentials)
        {
            ResetClient();

            InitializeClient(credentials);
        }

        private static void ResetClient()
        {
            try
            {
                if (Client != null && Client.IsConnected)
                {
                    Client.Disconnect();
                }

                ClientOptions clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };

                WebSocketClient customClient = new WebSocketClient(clientOptions);

                Client = new TwitchClient(customClient);
            }
            catch (Exception e)
            {
                Log.Error(e.Message + e.InnerException.Message);
            }
        }

        private static void InitializeClient(ConnectionCredentials credentials)
        {
            if (Client == null)
            {
                Log.Error("Tried to initialize null client, report to mod author");
                return;
            }

            // Initialize the client with the credentials instance, and setting a default channel to connect to.
            Client.Initialize(credentials, ToolkitCoreSettings.channel_username);

            // Bind Channel Connection
            Client.OnConnected += OnConnected;
            Client.OnJoinedChannel += OnJoinedChannel;

            // Bind Messages and Whispers
            Client.OnMessageReceived += OnMessageReceived;
            Client.OnWhisperReceived += OnWhisperReceived;
            Client.OnWhisperCommandReceived += OnWhisperCommandReceived;
            Client.OnChatCommandReceived += OnChatCommandReceived;

            // Bind Misc Events
            Client.OnBeingHosted += OnBeingHosted;
            Client.OnCommunitySubscription += OnCommunitySubscription;
            Client.OnConnectionError += OnConnectionError;
            Client.OnDisconnected += OnDisconnected;
            Client.OnFailureToReceiveJoinConfirmation += OnFailureToReceiveJoinConfirmation;
            Client.OnGiftedSubscription += OnGiftedSubscription;
            Client.OnHostingStarted += OnHostingStarted;
            Client.OnIncorrectLogin += OnIncorrectLogin;
            Client.OnLog += OnLog;
            Client.OnNewSubscriber += OnNewSubscriber;
            Client.OnReSubscriber += OnReSubscriber;
            Client.OnRaidNotification += OnRaidNotification;
            Client.OnUserBanned += OnUserBanned;


            Client.Connect();
        }

        private static void OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (Current.Game == null ||  !ToolkitCoreSettings.allowWhispers) return;

            List<TwitchInterfaceBase> receivers = Current.Game.components.OfType<TwitchInterfaceBase>().ToList();

            foreach (TwitchInterfaceBase receiver in receivers)
            {
                receiver.ParseMessage(e.WhisperMessage as ITwitchMessage);
            }

            MessageLog.LogMessage(e.WhisperMessage);
        }

        private static void OnWhisperCommandReceived(object sender, OnWhisperCommandReceivedArgs e)
        {
            if (Current.Game == null || !ToolkitCoreSettings.allowWhispers) return;

            ToolkitChatCommand chatCommand = ChatCommandController.GetChatCommand(e.Command.CommandText);

            if (chatCommand != null)
            {
                chatCommand.TryExecute(e.Command as ITwitchCommand);
            }
        }

        private static void OnConnected(object sender, OnConnectedArgs e)
        {
        }

        private static void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            if (ToolkitCoreSettings.sendMessageToChatOnStartup)
            {
                Client.SendMessage(e.Channel, "Toolkit Core has Connected to Chat");
            }
        }

        private static void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits > 0)
            {
                Log.Message("Bits donated : " + e.ChatMessage.Bits);
            }

            if (Current.Game == null) return;

            List<TwitchInterfaceBase> receivers = Current.Game.components.OfType<TwitchInterfaceBase>().ToList();

            foreach (TwitchInterfaceBase receiver in receivers)
            {
                receiver.ParseMessage(e.ChatMessage);
            }

            MessageLog.LogMessage(e.ChatMessage);
        }

        private static void OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (Current.Game == null || ToolkitCoreSettings.forceWhispers) return;

            ToolkitChatCommand chatCommand = ChatCommandController.GetChatCommand(e.Command.CommandText);

            if (chatCommand != null)
            {
                chatCommand.TryExecute(e.Command as ITwitchCommand);
            }
        }

        public static void OnBeingHosted(object sender, OnBeingHostedArgs e)
        {

        }

        public static void OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {

        }

        public static void OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Log.Error("Client has experienced a connection error. " + e.Error);
        }

        public static void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Log.Warning("Client has disconnected");
        }

        public static void OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {

        }

        public static void OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {

        }

        public static void OnHostingStarted(object sender, OnHostingStartedArgs e)
        {

        }

        public static void OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            Log.Error("Incorrect login detected. " + e.Exception.Message);
        }

        public static void OnLog(object sender, OnLogArgs e)
        {
            
        }

        public static void OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            Log.Message("New Subscriber. " + e.Subscriber.DisplayName);
        }

        public static void OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            Log.Message("New Subscriber. " + e.ReSubscriber.DisplayName);
        }

        public static void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            Log.Message("Being raided by " + e.RaidNotification.DisplayName);
        }

        public static void OnUserBanned(object sender, OnUserBannedArgs e)
        {
            Log.Message("User has been banned - " + e.UserBan.Username);
        }

        public static void SendChatMessage(string message)
        {
            Client.SendMessage(Client.GetJoinedChannel(ToolkitCoreSettings.channel_username), message);
        }
    }
}

