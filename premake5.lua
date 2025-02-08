HAZEL = os.getenv("HAZEL_DIR")

workspace "ImNotHungry"
	configurations
	{
		"Debug",
		"Release"
	}

group "Hazel"
	include (path.join(HAZEL, "Hazel", "vendor", "Coral", "Coral.Managed"))
	include (path.join(HAZEL, "Hazel-ScriptCore"))
group ""

project "ImNotHungry"
	location "Assets/Scripts"

	targetdir "%{prj.location}/Binaries"
	objdir "%{prj.location}/Intermediates"

	kind "SharedLib"
	language "C#"
	dotnetframework "net9.0"

	vsprops
	{
		AppendTargetFrameworkToOutputPath = "false",
		Nullable = "enable",
		CopyLocalLockFileAssemblies = "true",
		EnableDynamicLoading = "true"
	}

	files
	{
		"Assets/Scripts/Source/**.cs"
	}

	links
	{
		"Hazel-ScriptCore"
	}

	filter "configurations:Debug"
		optimize "Off"
		symbols "Default"
	filter ""
	
	filter "configurations:Release"
		optimize "On"
		symbols "Default"
	filter ""
