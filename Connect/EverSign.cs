using EverSign;
using EverSign.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Connect
{
    public static class EverSignFunctions
    {
        static string AccessToken = Environment.GetEnvironmentVariable("Eversign_AccessToken");
        static string Business_ID = "136688";


        //2/17/21 GOAL: Replace SendDocument, which uses local .json files to get field information, with EverSign's API templates.
        // SendDocument is a big function:
        /*
         * 1) Authenticates a new RestClient object.
         * 2) Reads a json string from FieldCoordinates.json, then parses if to see if its in a template env.
         * 3) Creates a new document body based on enum Favorites, a local file, and a title (generated from client.Name, client.ID, Favorite enum).
         * 4) Add signers depending on whether client is single or married.
         * 4.5) Extracts document fields from the json object by Favorite enum.
         * 5) Sends the document body to EverSign to be signed.
         * 
         * This functions has caused multiple problems:
         * - FieldCoordinates.json has to be changed before any new document body can be verified.
         * - Extremely tight coupling with married bool.
         * - Redundant and confusing functionality (what is the point of get the testing bool from json?)
         * 
         * Our goal is to replace two dependencies the function has: the Favorites enum and FieldCoordinates.json. Instead, EverSign itself will provide the information we need. This is how it'll look:
         * GetTemplates()
         * 1) Authenticates a new RestClient object.
         * 2) Gets templates from EverSign API.
         * 3) Organizes templates by title into List of (title, document_hash, bool married)
         * 4) Stores templates as Dictionary of templates.
         * 
         * User selects a template, a pdf, and a married check.
         * SendDocument()
         * 1) Authenticates a new RestClient object.
         * 2) Finds a template object based on married bool + template name. Extracts field information from template object.
         * 3) Creates a new document body from template fields, pdf file path, and title (generated from client.Name, client.ID, template.TemplateType)
         * 4) Add signers.
         * 5) Send.
         */

        /// <summary>
        /// Searches EverSign for templates, groups them by <see cref="TemplateCustomBody.TemplateType"/>. Married forms should have two documents in their grouping, rest have 1.
        /// </summary>
        /// <returns></returns>
        public static async Task<Dictionary<string, TemplateCustomBody>> GetTemplateGroup()
        {
            var client = new EverSignClient(AccessToken, Business_ID);
            return (await client.GetTemplates()).ToDictionary(X => X.TemplateType, X => X);
        }

        /// <summary>
        /// Prepares and sends a <see cref="DocumentBody"/> to EverSign for signing.
        /// </summary>
        /// <param name="template">The template chosen to extract fields from.</param>
        /// <param name="filepath">The file path of the document to be signed.</param>
        /// <param name="title">Title of document to be signed.</param>
        /// <param name="name">Name of first signer, the client.</param>
        /// <param name="email">Email of client.</param>
        /// <param name="spousename">If applicable, name of second signer, the spouse.</param>
        /// <param name="spouseemail">Email of second signer.</param>
        /// <returns>A list of embedded urls the signer(s) can use to sign the document.</returns>
        public static async Task<List<string>> SendDocument(TemplateCustomBody template, string filepath, string title, string name, string email, bool married,
            string spousename = null, string spouseemail = null)
        {
            //Checks if this is a debug document.
            bool testing = false;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                testing = true;
                email = "ramneek.194@gmail.com";
            }

            var client = new EverSignClient(AccessToken, Business_ID);

            //From the template, we can retrieve a few things...
            Field[][] fields = null;
            if (married)
            {
                fields = template.ExtractFields();
            }
            else
            {
                fields = template.ExtractSingleFields();
            }
            
            string message = "Global Tax Services thanks you for your business. Please sign the document to complete your appointment.";

            //Alright, now that we have the essentials, let's get create the document body.
            var docBody = DocumentBody.Create(filepath, title, message, fields, testing);

            //Based on marriage check, we'll add single or married signers.
            if (married)
            {
                docBody.AddMarriedSigners(name, email, spousename, spouseemail);
            }
            else
            {
                docBody.AddSigner(name, email);
            }

            return await client.UploadDocument(docBody);
        }
    }
}
