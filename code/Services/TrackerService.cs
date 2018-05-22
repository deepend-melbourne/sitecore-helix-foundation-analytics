using System;
using System.Globalization;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;
using Sitecore.Diagnostics;
using Sitecore.Foundation.Analytics.Helpers;
using Sitecore.Foundation.DependencyInjection;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Marketing.Definitions.Outcomes.Model;
using Sitecore.Marketing.Definitions.PageEvents;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;

namespace Sitecore.Foundation.Analytics.Services
{
    [Service]
    public class TrackerService
    {
        readonly IDefinitionManager<IPageEventDefinition> _pageEventDefinitionManager;
        readonly IDefinitionManager<IGoalDefinition> _goalDefinitionManager;
        readonly IDefinitionManager<IOutcomeDefinition> _outcomeDefinitionManager;

        public TrackerService(IDefinitionManager<IPageEventDefinition> pageEventDefinitionManager, IDefinitionManager<IGoalDefinition> goalDefinitionManager, IDefinitionManager<IOutcomeDefinition> outcomeDefinitionManager)
        {
            _pageEventDefinitionManager = pageEventDefinitionManager;
            _goalDefinitionManager = goalDefinitionManager;
            _outcomeDefinitionManager = outcomeDefinitionManager;
        }

        public bool IsActive
        {
            get
            {
                if (Tracker.Enabled == false || Tracker.Current == null)
                {
                    return false;
                }

                if (!Tracker.Current.IsActive)
                {
                    Tracker.StartTracking();
                }

                return true;
            }
        }

        public virtual void TrackPageEvent(Guid pageEventId, string text = null, string data = null, string dataKey = null, int? value = null)
        {
            Assert.ArgumentNotNull(pageEventId, nameof(pageEventId));

            if (!IsActive)
            {
                return;
            }

            var pageEventDefinition = _pageEventDefinitionManager.Get(pageEventId, CultureInfo.InvariantCulture);
            if (pageEventDefinition == null)
            {
                Log.Warn($"Cannot find page event: {pageEventId}", this);
                return;
            }

            var eventData = Tracker.Current.CurrentPage.RegisterPageEvent(pageEventDefinition);
            if (data != null)
            {
                eventData.Data = data;
            }

            if (dataKey != null)
            {
                eventData.DataKey = dataKey;
            }

            if (text != null)
            {
                eventData.Text = text;
            }

            if (value != null)
            {
                eventData.Value = value.Value;
            }
        }

        public void TrackGoal(Guid goalId, string text = null, string data = null, string dataKey = null, int? value = null)
        {
            Assert.ArgumentNotNull(goalId, nameof(goalId));

            if (!IsActive)
            {
                return;
            }

            var goalDefinition = _goalDefinitionManager.Get(goalId, CultureInfo.InvariantCulture);
            if (goalDefinition == null)
            {
                Log.Warn($"Cannot find goal: {goalId}", this);
                return;
            }

            var eventData = Tracker.Current.CurrentPage.RegisterGoal(goalDefinition);
            if (data != null)
            {
                eventData.Data = data;
            }

            if (dataKey != null)
            {
                eventData.DataKey = dataKey;
            }

            if (text != null)
            {
                eventData.Text = text;
            }

            if (value != null)
            {
                eventData.Value = value.Value;
            }
        }

        public void TrackOutcome(Guid outComeDefinitionId, string currencyCode, decimal monetaryValue)
        {
            Assert.ArgumentNotNull(outComeDefinitionId, nameof(outComeDefinitionId));

            if (!IsActive || Tracker.Current.Contact == null)
            {
                return;
            }

            var outcomeDefinition = _outcomeDefinitionManager.Get(outComeDefinitionId, CultureInfo.InvariantCulture);
            if (outcomeDefinition == null)
            {
                Log.Warn($"Cannot find outcome: {outComeDefinitionId}", this);
                return;
            }

            Tracker.Current.CurrentPage.RegisterOutcome(outcomeDefinition, currencyCode, monetaryValue);
        }

        public void IdentifyContact(XConnectClient client, string source, string identifier, bool force = false)
        {
            if (!IsActive)
            {
                return;
            }

            if (Configuration.Factory.CreateObject("tracking/contactManager", true) is Sitecore.Analytics.Tracking.ContactManager manager)
            {
                if (force || Tracker.Current.Contact.IsNew || Tracker.Current.Contact.IdentificationLevel == ContactIdentificationLevel.Anonymous)
                {
                    Tracker.Current.Contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
                    manager.SaveContactToCollectionDb(Tracker.Current.Contact);
                    manager.AddIdentifier(Tracker.Current.Contact.ContactId, new Sitecore.Analytics.Model.Entities.ContactIdentifier(source, identifier, ContactIdentificationLevel.Known));

                    // Now that the contact is saved, you can retrieve it using the tracker identifier
                    // NOTE: Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource is marked internal in 9.0 Initial - use "xDB.Tracker"
                    var trackerIdentifier = new IdentifiedContactReference(Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource, Tracker.Current.Contact.ContactId.ToString("N"));

                    try
                    {
                        var contact = client.Get(trackerIdentifier, new ContactExpandOptions());

                        if (contact != null)
                        {
                            manager.RemoveFromSession(Tracker.Current.Contact.ContactId);
                            Tracker.Current.Session.Contact = manager.LoadContact(Tracker.Current.Contact.ContactId);
                        }
                    }
                    catch (XdbExecutionException)
                    {
                        // Manage conflicts / exceptions
                    }
                }
            }
        }
    }
}
