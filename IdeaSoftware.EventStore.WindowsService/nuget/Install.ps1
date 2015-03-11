param($installPath, $toolsPath, $package, $project)
Process 
{	
	function AddConfig($configPath, $exePath)
	{
		if(test-path $configpath)
		{
			Write-Host $configPath
			Write-Host $exePath
			$configXml = [xml] (Get-Content $configPath)
			$node = $config.configuration.appSettings.add | where { $_.key -eq 'ES:ExePath' }
			if($node -eq $null )
			{
				$appSettings = $configXml.SelectSingleNode('configuration/appSettings')
				#insert appSettings node is missing
				if($appSettings -eq $null)
				{
					$appSettingsElem = $configXml.CreateElement('appSettings')
					$configElem = $configXml.SelectSingleNode('configuration')
					$configElem.AppendChild($appSettingsElem)
					$configXml.save($configPath)
				}
				$appSettings = $configXml.SelectSingleNode('configuration/appSettings')
				$add = $configXml.CreateElement('add')
				$add.SetAttribute('key', 'ES:ExePath')
				$add.SetAttribute('value', $exePath)
				
				$appSettings.AppendChild($add)
				$configXml.Save($configPath)
			}
		}
	}
	
	AddConfig "$installPath\..\..\$project\App.config" "$installPath\EventStore\EventStore.ClusterNode.exe"
	AddConfig "$installPath\..\..\$project\Web.config" "$installPath\EventStore\EventStore.ClusterNode.exe"
	
	
} 
