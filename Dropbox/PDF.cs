using Connect.ClientBase;
using Logger;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Connect.PDFEdit
{
    public class PDF
    {
        static PDF()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public enum PDFType
        {
            Checklist = 1,
            Contract = 2,
        }

        private static string[] BadFields =
        {
            "PdfSharp.Pdf.AcroForms.PdfSignatureField",
            "PdfSharp.Pdf.AcroForms.PdfPushButtonField",
            "PdfSharp.Pdf.AcroForms.PdfButtonField"
        };

        public struct FieldPair
        {
            public string Value;
            public bool ReadOnly;

            public FieldPair(string value, bool readOnly = true)
            {
                Value = value;
                ReadOnly = readOnly;
            }
        }

        public static Dictionary<string, PDF.FieldPair> CreateChecklist(Client client)
        {
            var val = new Dictionary<string, PDF.FieldPair>();
            val.Add("Name", new PDF.FieldPair(client.Name, false));
            val.Add("Email", new PDF.FieldPair(client.Email, false));
            val.Add("Phone", new PDF.FieldPair(client.Phone, false));
            val.Add("ClientFolder", new PDF.FieldPair(client.InternalData?.DropboxFolderPath ?? @"/Data/ClientChecklist", true));
            val.Add("DropboxSharedURL", new PDF.FieldPair(client.DropboxSharedURL, true));
            val.Add("ID", new PDF.FieldPair(client.ID.ToString(), true));

            val.Add("SpouseName", new PDF.FieldPair(client.InternalData?.SpouseName ?? "", false));

            return val;
        }

        public static Dictionary<string, PDF.FieldPair> CreateContract(Client client)
        {
            var val = new Dictionary<string, PDF.FieldPair>();
            val.Add("clientname", new FieldPair(client.Name, false));
            val.Add("clientemail", new FieldPair(client.Email, false));
            val.Add("clientphone", new PDF.FieldPair(client.Phone, false));
            val.Add("ClientFolder", new PDF.FieldPair(client.InternalData?.DropboxFolderPath ?? @"/Data/ClientSubmission", true));
            val.Add("DropboxSharedURL", new PDF.FieldPair(client.DropboxSharedURL, true));
            val.Add("ID", new PDF.FieldPair(client.ID.ToString(), true));

            val.Add("day", new FieldPair(DateTime.Today.Day.ToString(), false));
            val.Add("month", new FieldPair(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Today.Month), false));
            val.Add("year", new FieldPair(DateTime.Today.Year.ToString().Substring(2, 2), false));
            val.Add("compensation", new PDF.FieldPair(client.ContractData.total, true));

            for (int i = 1; i < 6; i++)
            {
                val.Add("Row" + i.ToString(), new FieldPair(i.ToString(), true));
                val.Add("ServiceRow" + i.ToString(), new FieldPair(client.ContractData.services[i - 1].servicetype, true));
                val.Add("DescriptionRow" + i.ToString(), new FieldPair(client.ContractData.services[i - 1].description, true));
                val.Add("Cost" + i.ToString(), new FieldPair(client.ContractData.services[i - 1].cost, true));
            }
            return val;
        }

        public static Stream FillPDF(PDFType type, Dictionary<string, FieldPair> formvalues)
        {
            string inputDoc;
            if (type == PDFType.Checklist)
            {
                inputDoc = FilePaths.Templates + "Personal Tax Checklist.pdf";
            }
            else if (type == PDFType.Contract)
            {
                inputDoc = FilePaths.Templates + "Tax Contract.pdf";
            }
            else
            {
                var exc = new FileNotFoundException("File could not be found for " + type.ToString(), type.ToString());
                throw exc;
            }

            var memStream = new MemoryStream();
            using (var outputStream = new FileStream(inputDoc, FileMode.Open))
            {
                outputStream.CopyTo(memStream);
            }

            List<string> badFields = new List<string>(BadFields);
            var doc = PdfReader.Open(memStream, PdfDocumentOpenMode.Modify);

            if (doc.AcroForm.Elements.ContainsKey("/NeedAppearances"))
            { doc.AcroForm.Elements["/NeedAppearances"] = new PdfSharp.Pdf.PdfBoolean(true); }
            else { doc.AcroForm.Elements.Add("/NeedAppearances", new PdfSharp.Pdf.PdfBoolean(true)); }

            foreach (var fieldvalue in formvalues)
            {
                var field = doc.AcroForm.Fields[fieldvalue.Key];

                if (field is null)
                {
                    Log.error("Field named " + fieldvalue.Key + " could not be found. Proceeding to next value.");
                    continue;
                }
                if (!badFields.Contains(field.GetType().FullName))
                {
                    field.ReadOnly = false;
                    field.Value = new PdfString(formvalues[field.Name].Value);
                    field.ReadOnly = formvalues?[field.Name].ReadOnly ?? false;
                    //Log.info(field.Value.ToString()); Unnecessary logging.
                }
            }

            doc.Info.Author = "Ramneek Singh";
            doc.Info.CreationDate = DateTime.Now;
            doc.SecuritySettings.PermitModifyDocument = false;
            doc.SecuritySettings.OwnerPassword = DropboxClientLibrary.Properties.Resources.password;

            doc.Save(memStream, false);
            return memStream;
        }
    }
}