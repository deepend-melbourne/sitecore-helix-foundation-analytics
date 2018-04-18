using System;
using System.Threading.Tasks;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;

namespace Sitecore.Foundation.Analytics.Extensions
{
    public static class ContactExtensions
    {
        public static Contact ToContact(this Sitecore.Analytics.Tracking.Contact contact, XConnectClient client, string[] facets = null)
        {
            var trackerIdentifier = new IdentifiedContactReference(
                Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource,
                contact.ContactId.ToString("N"));

            var ret = client.Get(trackerIdentifier, new ContactExpandOptions(facets));

            if (ret == null)
            {
                ret = new Contact(new ContactIdentifier(Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource, contact.ContactId.ToString("N"), ContactIdentifierType.Anonymous));

                client.AddContact(ret);

                client.Submit();
            }

            return ret;
        }

        public static async Task<Contact> ToContactAsync(this Sitecore.Analytics.Tracking.Contact contact, XConnectClient client, string[] facets = null)
        {
            var trackerIdentifier = new IdentifiedContactReference(
                Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource,
                contact.ContactId.ToString("N"));

            var ret = await client.GetAsync(trackerIdentifier, new ContactExpandOptions(facets));

            if (ret == null)
            {
                ret = new Contact(new ContactIdentifier(Sitecore.Analytics.XConnect.DataAccess.Constants.IdentifierSource, contact.ContactId.ToString("N"), ContactIdentifierType.Anonymous));

                client.AddContact(ret);

                await client.SubmitAsync();
            }

            return ret;
        }

        public static TFacet GetOrCreateFacet<TFacet>(this Contact contact, string facetKey)
            where TFacet : Facet, new()
        {
            var facet = contact.GetFacet<TFacet>(facetKey);
            if (facet == null)
            {
                return new TFacet();
            }

            return facet;
        }

        public static Contact CreateOrUpdateFacet<TFacet>(this Contact contact, XConnectClient client, string facetKey, Action<TFacet> update)
            where TFacet : Facet, new()
        {
            var facet = GetOrCreateFacet<TFacet>(contact, facetKey);

            update(facet);

            client.SetFacet(contact, facetKey, facet);

            return contact;
        }
    }
}
