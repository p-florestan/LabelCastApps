
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using LabelCast;

namespace LabelCastDesktop
{
    public class App
    {
        #region Fields 

        // In the App class, we use native Profile/Printer classes (not the DTOs)
        private LabelConfig mConf = new LabelConfig();
        private Profile? mActiveProfile;
        private Printer? mActivePrinter;
        private LabelProcessor? mLabelProcessor;
        private DataTable mFieldTable = new DataTable();

        #endregion

        #region Constructors 

        public App()
        {
            // Logging
            // We always start in Debug log level, and once full configuration is read,
            // it is set to what is contained in "Client.json" config file
            Logger.CurrentLogLevel = Level.Debug;
            Logger.CurrentLogFile = @"C:\Program Info\LabelCast\Logs\DesktopAppLog.txt";
            Logger.Write(Level.Notice, "");
            Logger.Write(Level.Notice, "LabelCastDesktop application started.");

            // Decouple initialization
            var initTimer = new System.Timers.Timer { AutoReset = false, Interval = 10 };
            initTimer.Elapsed += InitTimer_Elapsed;
            initTimer.Start();            
        }

        #endregion

        #region Initialization

        /// <summary>
        /// When the startup timer elapses we initialize the application.
        /// </summary>
        private void InitTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            mConf = new LabelConfig();
            mConf.ReadConfiguration();

            mActiveProfile = mConf.ActiveProfile;
            mActivePrinter = mConf.ActivePrinter;
            if (mActiveProfile == null)
            {
                TriggerAppMessageEvent("Invalid configuration - no active configuration profile found.");
                return;
            }

            try
            {
                mLabelProcessor = new LabelProcessor(mActiveProfile, mActivePrinter);
                mLabelProcessor.MessageEvent += OnLabelPrintMessage;
                mLabelProcessor.PrintCompleteEvent += OnLabelPrintComplete;
                mLabelProcessor.InitializeDatabase();
                mFieldTable = mLabelProcessor.GetFieldTable();
            }
            catch (Exception ex)
            {
                TriggerAppMessageEvent(ex.Message);
                MessageBox.Show("Fatal Error: " + ex.Message);
                return;
            }

            TriggerProfileUpdate();
        }

        #endregion

        #region Events for Windows Form 

        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public delegate void ProfileEventHandler(object sender, ProfileEventArgs e);
        public delegate void LabelEventHandler(object sender, EventArgs e);
        public delegate void LabelPrintCompleteEventHandler(object sender, EventArgs e);

        public event MessageEventHandler? MessageEvent;
        public event ProfileEventHandler? ProfileEvent;
        public event LabelEventHandler? LabelEvent;
        public event LabelPrintCompleteEventHandler? LabelPrintCompleteEvent;

        /// <summary>
        /// Event for notification messages.
        /// </summary>
        protected virtual void TriggerAppMessageEvent(String message)
        {
            Logger.Write(Level.Debug, "TriggerAppMessage: " + message);
            MessageEvent?.Invoke(this, new MessageEventArgs(message));
        }

        /// <summary>
        /// Fires when a label has completed printing (i.e. data was sent to the printer).
        /// </summary>
        /// <param name="message"></param>
        protected virtual void TriggerPrintCompleteEvent()
        {
            Logger.Write(Level.Debug, "TriggerLabelPrintComplete");
            LabelPrintCompleteEvent?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event for update profile updates (including printers).<br/>
        /// Fire this when loading/changing profile or printer data.
        /// </summary>
        protected virtual void TriggerProfileUpdate()
        {
            // Decouple the profile-lists (deep copy) 
            List<Profile> tmpProfiles = GetSavedProfiles();
            // ... but not the printers - this is a global object
            ProfileEvent?.Invoke(this, new ProfileEventArgs(tmpProfiles, PrinterStore.Printers));
        }

        /// <summary>
        /// Event to update the Label-Printing tab of the UI when the active profile changes.
        /// </summary>
        protected virtual void TriggerProfileChangedEvent()
        {
            LabelEvent?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// This method is invoked when an event from the BarcodeLabel class is fired.
        /// It should in turn trigger the message event on the WinForm.
        /// </summary>
        private void OnLabelPrintMessage(object sender, LabelCast.MessageEventArgs e)
        {
            TriggerAppMessageEvent(e.Message);
        }

        private void OnLabelPrintComplete(object sender, EventArgs e)
        {
            TriggerPrintCompleteEvent();
        }


        #endregion

        #region Public Properties

        public LabelProcessor? BarcodeLabelProcessor
        {
            get { return mLabelProcessor; }
        }

        public DataTable FieldTable
        {
            get { return mFieldTable; }
        }

        public bool IsConfigurationLocked
        {
            get { return mConf.IsLocked; }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Save temporary profile configuration as modified on PropertyGrid on the Form.
        /// This also saves printer data at the same time. 
        /// </summary>
        public String SaveConfiguration(List<Profile> profileList, List<Printer> printerList)
        {
            mConf.ProfileList = profileList;
            PrinterStore.ReplaceAllPrinters(printerList);
            String message = mConf.SaveConfiguration();
            if (!String.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show("Cannot save configuration - invalid data:\r\n\r\n" + message);
                return "Could not save configuration";
            }
            else
            {
                // Set active profile + printer again to recreate LabelProcessor with new settings
                if (mActiveProfile != null)
                    SetActiveProfile(mActiveProfile.Abbreviation);
                if (mActivePrinter != null)
                    SetActivePrinter(mActivePrinter.Name);
                //
                return "Configuration saved.";
            }
        }

        public void SetActiveProfile(String activeProfileAbbr)
        {
            mActiveProfile = mConf.ProfileList.FirstOrDefault(p => p.Abbreviation == activeProfileAbbr);
            if (mActiveProfile == null)
            {
                TriggerAppMessageEvent("Invalid profile abbreviation: '" + activeProfileAbbr + "'. Cannot change.");
                return;
            }
            else
            {
                if (mActivePrinter == null)
                    mActivePrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name == mActiveProfile.DefaultPrinter);
                if (mActivePrinter == null)
                {
                    TriggerAppMessageEvent("You must assign a default printer to the profile!");
                    return;
                }
                else
                {
                    mLabelProcessor = new LabelProcessor(mActiveProfile, mActivePrinter);
                    mFieldTable = mLabelProcessor.GetFieldTable();
                    TriggerProfileChangedEvent();

                    String message = mConf.SaveActiveProfileAndPrinter(activeProfileAbbr, mActivePrinter.Name);
                    if (!String.IsNullOrWhiteSpace(message))
                    {
                        TriggerAppMessageEvent(message);
                    }
                }
            }
        }


        public void SetActivePrinter(String printerName)
        {
            if (mActiveProfile == null)
            {
                TriggerAppMessageEvent("Internal error - currently active profile is null. Restart the app.");
                return;
            }

            mActivePrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name == printerName);
            if (mActivePrinter == null)
            {
                TriggerAppMessageEvent("Invalid printer name: '" + printerName + "'. Cannot change.");
                return;
            }
            else
            {
                mLabelProcessor = new LabelProcessor(mActiveProfile, mActivePrinter);
                mFieldTable = mLabelProcessor.GetFieldTable();
                TriggerProfileChangedEvent();

                String message = mConf.SaveActiveProfileAndPrinter(mActiveProfile.Abbreviation, printerName);
                if (!String.IsNullOrWhiteSpace(message))
                {
                    TriggerAppMessageEvent(message);
                }
            }
        }


        /// <summary>        
        /// Obtain a list of the saved profiles (original configuration as loaded from disk).
        /// </summary>
        public List<Profile> GetSavedProfiles()
        {
            // deep copy (to decouple the list objects)
            List<Profile> tmpProfiles = new List<Profile>();
            foreach (Profile profile in mConf.ProfileList)
            {
                tmpProfiles.Add(profile);
            }
            return tmpProfiles;
        }


        public Profile? GetActiveProfile()
        {
            return mActiveProfile;
        }


        public String GetActiveProfileName()
        {
            return mActiveProfile?.Name ?? String.Empty;
        }

        public String GetActiveProfileAbbr()
        {
            return mActiveProfile?.Abbreviation ?? String.Empty;
        }

        public String GetActivePrinterName()
        {
            return mActivePrinter?.Name ?? String.Empty;
        }

        #endregion
    }


    #region Custom EventArg Classes 

    public class MessageEventArgs : EventArgs
    {
        public String Message { get; set; } = "";

        public MessageEventArgs() { }

        public MessageEventArgs(String message)
        {
            this.Message = message;
        }
    }

    public class ProfileEventArgs : EventArgs
    {
        public List<Profile> Profiles { get; set; } = new List<Profile>();
        public List<Printer> Printers { get; set; } = new List<Printer>();

        public ProfileEventArgs() { }

        public ProfileEventArgs(List<Profile> profiles, List<Printer> printers)
        {
            this.Profiles = profiles;
            this.Printers = printers;
        }
    }

    #endregion
}
