using System;
using System.Collections.Generic;
using System.Text;
using RSS_Report_Retrievers.RSS;
using RSS_Report_Retrievers.Classes;

namespace RSS_Report_Retrievers.Classes
{
    public class RS2000Facade : IRSFacade
    {
        private ReportingService rs = new ReportingService();

        private string baseUrl;

        public string BaseUrl
        {
            get { return baseUrl; }
            set { baseUrl = value; }
        }
	

        #region IRSFacade Members

        public void CreateFolder(string Folder, string text, string properties)
        {
            rs.CreateFolder(Folder, text, null);
        }

        private static ReportWarning ConvertSPWarningToReportWarning(Warning w)
        {
            ReportWarning returnWarning;
            returnWarning.Code = w.Code;
            returnWarning.Message = w.Message;
            returnWarning.ObjectName = w.ObjectName;
            returnWarning.ObjectType = w.ObjectType;
            returnWarning.Severity = w.Severity;

            return returnWarning;
        }

        public ReportWarning[] CreateReport(string filename, string destination, bool overwrite, byte[] definition, string Properties)
        {
            RSS.Warning[] w = rs.CreateReport(System.IO.Path.GetFileNameWithoutExtension(filename), destination, overwrite, definition, null);
            return System.Array.ConvertAll<Warning, ReportWarning>(w, ConvertSPWarningToReportWarning);
                
        }

        public ReportWarning[] CreateModel(string filename, string destination, byte[] definition, string Properties)
        {
            return null;
        }

        public System.Net.ICredentials Credentials
        {
            get
            {
                return rs.Credentials;
            }
            set
            {
                rs.Credentials = value;
            }
        }

        public void DeleteItem(string path)
        {
            rs.DeleteItem(path);
        }

        public List<List<string>> GetItemProperties(string path)
        {
            List<List<String>> properties = new List<List<string>>();
            foreach (Property property in rs.GetProperties(path, null))
            {
                List<String> propertieinfo = new List<String>();
                propertieinfo.Add(property.Name);
                propertieinfo.Add(property.Value);
                properties.Add(propertieinfo);
            }

            return properties;
        }

        public List<List<string>> GetItemSecurity(string path)
        {
            bool inheritParent;

            List<List<String>> permissions = new List<List<string>>();
            foreach (Policy permission in rs.GetPolicies(path, out inheritParent))
            {
                List<String> permissioninfo = new List<String>();
                string roles = "";
                permissioninfo.Add(permission.GroupUserName);
                foreach (Role role in permission.Roles)
                {
                    roles += role.Name + ",";
                }
                permissioninfo.Add(inheritParent.ToString());
                permissioninfo.Add(roles.TrimEnd(','));
                permissions.Add(permissioninfo);
            }

            return permissions;
        }

        public byte[] GetModelDefinition(string path)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public List<string> GetReportDatasources(string path)
        {
            List<String> datasources = new List<string>();
            foreach (DataSource ds in rs.GetReportDataSources(path))
            {
                datasources.Add(ds.Name);
            }

            return datasources;
        }

        public byte[] GetReportDefinition(string path)
        {
            return rs.GetReportDefinition(path);
        }

        public List<List<string>> GetReportParameters(string path)
        {
            List<List<String>> parameters = new List<List<string>>();

            foreach (RSS.ReportParameter parameter in rs.GetReportParameters(path, null, false, null, null))
            {
                List<String> parameterinfo = new List<String>();
                parameterinfo.Add(parameter.Name);
                parameterinfo.Add(parameter.Type.ToString());
                parameterinfo.Add(parameter.AllowBlank.ToString());
                parameterinfo.Add(parameter.Nullable.ToString());
                parameterinfo.Add(parameter.MultiValue.ToString());
                parameterinfo.Add(parameter.Prompt);
                parameters.Add(parameterinfo);
            }

            return parameters;
        }

        public ReportItemDTO[] ListChildren(string item, bool recursive)
        {
            return Array.ConvertAll<CatalogItem,ReportItemDTO>(rs.ListChildren(item, recursive),ConvertCatalogItemToReportItemDTO);
        }

        public void MoveItem(string source, string destination, ReportItemTypes type)
        {
            rs.MoveItem(source, destination.StartsWith("/") ? destination : "/" + destination);
        }

        public void SetItemDataSources(string item, string dataSourceName)
        {
            foreach (DataSource availableDataSource in rs.GetReportDataSources(item))
            {
                // Only update the report when the selected datasource is used in that report
                if (availableDataSource.Name == dataSourceName)
                {
                    try
                    {
                        DataSourceReference dsr = new DataSourceReference();
                        dsr.Reference = dataSourceName;

                        DataSource[] dataSources = new DataSource[1];

                        DataSource ds = new DataSource();
                        ds.Item = (DataSourceDefinitionOrReference)dsr;
                        ds.Name = ("/" + dataSourceName.Split('/')[dataSourceName.Split('/').GetUpperBound(0)]).Trim('/');
                        dataSources[0] = ds;

                        rs.SetReportDataSources(item, dataSources);

                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("An error has occured: {0}", ex.Message));
                    }
                }
            }
        }

        #endregion
        private static ReportItemTypes GetReportItemTypeFromSSRSItemTyp(ItemTypeEnum ssrsType)
        {
            ReportItemTypes convertedType = ReportItemTypes.Unknown;

            switch (ssrsType)
            {
                case ItemTypeEnum.Folder: convertedType = ReportItemTypes.Folder; break;
                case ItemTypeEnum.Report: convertedType = ReportItemTypes.Report; break;
                case ItemTypeEnum.DataSource: convertedType = ReportItemTypes.Datasource; break;
            }

            return convertedType;
        }
        private static ReportItemDTO ConvertCatalogItemToReportItemDTO(CatalogItem item)
        {
            ReportItemDTO returnItem;

            returnItem.Hidden = item.Hidden;
            returnItem.Name = item.Name;
            returnItem.Path = item.Path;
            returnItem.Type = GetReportItemTypeFromSSRSItemTyp(item.Type);

            return returnItem;
        }

        private static CredentialRetrievalEnum GetSSRSCredentialRetrievalTypeFromEnum(CredentialRetrievalTypes type)
        {
            CredentialRetrievalEnum convertedType = CredentialRetrievalEnum.None;

            switch (type)
            {
                case CredentialRetrievalTypes.Integrated: convertedType = CredentialRetrievalEnum.Integrated; break;
                case CredentialRetrievalTypes.None: convertedType = CredentialRetrievalEnum.None; break;
                case CredentialRetrievalTypes.Prompt: convertedType = CredentialRetrievalEnum.Prompt; break;
                case CredentialRetrievalTypes.Store: convertedType = CredentialRetrievalEnum.Store; break;
            }

            return convertedType;
        }

        public void CreateDataSource(Datasource datasource, string parent)
        {
            DataSourceDefinition def = new DataSourceDefinition();
            def.ConnectString = datasource.ConnectionString;
            def.Enabled = datasource.Enabled;
            def.Extension = datasource.Extension;
            def.Password = datasource.Password;
            def.Prompt = datasource.Prompt;
            def.UserName = datasource.Username;
            def.ImpersonateUser = datasource.SetExecutionContext;
            def.WindowsCredentials = datasource.UsePromptedCredentialsAsWindowsCredentials || datasource.UseStoredCredentialsAsWindowsCredentials;
            def.CredentialRetrieval = GetSSRSCredentialRetrievalTypeFromEnum(datasource.CredentialRetrievalType);

            rs.CreateDataSource(datasource.Name, parent, true, def, null);
        }

        public List<DatasourceExtension> GetDataExtensions()
        {
            List<DatasourceExtension> extensions = new List<DatasourceExtension>();

            foreach (Extension extension in rs.ListExtensions(ExtensionTypeEnum.Data))
            {
                extensions.Add(new DatasourceExtension(extension.Name, extension.LocalizedName));
            }

            return extensions;
        }

        private static CredentialRetrievalTypes GetCredentialRetrievalTypeFromSSRSType(CredentialRetrievalEnum type)
        {
            CredentialRetrievalTypes convertedType = CredentialRetrievalTypes.None;

            switch (type)
            {
                case CredentialRetrievalEnum.Integrated: convertedType = CredentialRetrievalTypes.Integrated; break;
                case CredentialRetrievalEnum.None: convertedType = CredentialRetrievalTypes.None; break;
                case CredentialRetrievalEnum.Prompt: convertedType = CredentialRetrievalTypes.Prompt; break;
                case CredentialRetrievalEnum.Store: convertedType = CredentialRetrievalTypes.Store; break;
            }

            return convertedType;
        }

        public Datasource GetDatasource(string path)
        {
            DataSourceDefinition def = rs.GetDataSourceContents(path);
            Datasource ds = new Datasource();
            ds.ConnectionString = def.ConnectString;
            ds.CredentialRetrievalType = GetCredentialRetrievalTypeFromSSRSType(def.CredentialRetrieval);
            ds.Enabled = def.Enabled;
            ds.Extension = def.Extension;
            ds.Username = def.UserName;
            ds.Password = def.Password;
            ds.Prompt = def.Prompt;
            ds.SetExecutionContext = def.ImpersonateUser;
            ds.UseStoredCredentialsAsWindowsCredentials = def.WindowsCredentials;
            ds.UsePromptedCredentialsAsWindowsCredentials = def.WindowsCredentials;

            return ds;
        }

        #region IRSFacade Members


        public List<ReportItemDTO> ListDependantItems(string reportModelpath)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}