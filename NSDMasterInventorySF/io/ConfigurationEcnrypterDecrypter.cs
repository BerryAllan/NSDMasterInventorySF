using System.Configuration;

namespace NSDMasterInventorySF.io
{
	public class ConfigurationEcnrypterDecrypter
	{
		/*
		// Protect the connectionStrings section.
		public static void ProtectConfiguration()
		{
			// Get the application configuration file.
			var config = ConfigurationManager.OpenExeConfiguration(
				ConfigurationUserLevel.None);

			// Define the Rsa provider name.
			var provider = "RsaProtectedConfigurationProvider";

			// Get the section to protect.
			ConfigurationSection connStrings = config.ConnectionStrings;

			if (connStrings != null)
				if (!connStrings.SectionInformation.IsProtected)
					if (!connStrings.ElementInformation.IsLocked)
					{
						// Protect the section.
						connStrings.SectionInformation.ProtectSection(provider);

						connStrings.SectionInformation.ForceSave = true;
						config.Save(ConfigurationSaveMode.Full);

						//Console.WriteLine("Section {0} is now protected by {1}",
						//	connStrings.SectionInformation.Name,
						//	connStrings.SectionInformation.ProtectionProvider.Name);
					}
					else
					{
						Console.WriteLine(
							"Can't protect, section {0} is locked",
							connStrings.SectionInformation.Name);
					}
				else
					Console.WriteLine(
						"Section {0} is already protected by {1}",
						connStrings.SectionInformation.Name,
						connStrings.SectionInformation.ProtectionProvider.Name);
			else
				Console.WriteLine("Can't get the section {0}",
					connStrings.SectionInformation.Name);
		}

		// Unprotect the connectionStrings section.
		public static void UnProtectConfiguration()
		{
			// Get the application configuration file.
			var config =
				ConfigurationManager.OpenExeConfiguration(
					ConfigurationUserLevel.None);

			// Get the section to unprotect.
			ConfigurationSection connStrings =
				config.ConnectionStrings;

			if (connStrings != null)
				if (connStrings.SectionInformation.IsProtected)
					if (!connStrings.ElementInformation.IsLocked)
					{
						// Unprotect the section.
						connStrings.SectionInformation.UnprotectSection();

						connStrings.SectionInformation.ForceSave = true;
						config.Save(ConfigurationSaveMode.Full);

						Console.WriteLine("Section {0} is now unprotected.",
							connStrings.SectionInformation.Name);
					}
					else
					{
						Console.WriteLine(
							"Can't unprotect, section {0} is locked",
							connStrings.SectionInformation.Name);
					}
				else
					Console.WriteLine(
						"Section {0} is already unprotected.",
						connStrings.SectionInformation.Name);
			else
				Console.WriteLine("Can't get the section {0}",
					connStrings.SectionInformation.Name);
		}
		*/

		public static void EncryptConfig()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			ConfigurationSection configSection = config.GetSection("userSettings/NSDMasterInventorySF.Properties.Settings");
			if (configSection != null)
				if (!configSection.SectionInformation.IsProtected)
					if (!configSection.ElementInformation.IsLocked)
					{
						//DataProtectionConfigurationProvider
						configSection.SectionInformation.ProtectSection("RsaProtectedConfigurationProvider");
						configSection.SectionInformation.ForceSave = true;
						config.Save(ConfigurationSaveMode.Full);
					}

			//foreach (ConfigurationSection section in config.Sections)
			//	if (section != null)
			//		if (!section.SectionInformation.IsProtected)
			//			if (!section.ElementInformation.IsLocked)
			//			{
			//				try
			//				{
			//					section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
			//					section.SectionInformation.ForceSave = true;
			//					config.Save(ConfigurationSaveMode.Full);
			//				} catch{}
			//			}
		}

		public static void UnEncryptConfig()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			ConfigurationSection configSection = config.GetSection("userSettings/NSDMasterInventorySF.Properties.Settings");
			if (configSection != null)
				if (!configSection.SectionInformation.IsProtected)
					if (!configSection.ElementInformation.IsLocked)
					{
						configSection.SectionInformation.UnprotectSection();
						configSection.SectionInformation.ForceSave = true;
						config.Save(ConfigurationSaveMode.Full);
					}

			//foreach (ConfigurationSection section in config.Sections)
			//	if (section != null)
			//		if (!section.SectionInformation.IsProtected)
			//			if (!section.ElementInformation.IsLocked)
			//			{
			//				section.SectionInformation.UnprotectSection();
			//				section.SectionInformation.ForceSave = true;
			//				config.Save(ConfigurationSaveMode.Full);
			//			}
		}

		/*
		public static void Main(string[] args)
		{
			var selection = string.Empty;

			if (args.Length == 0)
			{
				Console.WriteLine(
					"Select protect or unprotect");
				return;
			}

			selection = args[0].ToLower();

			switch (selection)
			{
				case "protect":
					ProtectConfiguration();
					break;

				case "unprotect":
					UnProtectConfiguration();
					break;

				default:
					Console.WriteLine("Unknown selection");
					break;
			}

			Console.Read();
		}
		*/
	}
}