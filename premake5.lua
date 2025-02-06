-- Solution Configuration
workspace "DaveSolution"
    configurations { "Debug", "Release" }
    platforms { "Any CPU" }

    -- Global settings
    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

    filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"

    filter {}

-- C# Frontend Project
project "Dave"
    kind "WindowedApp"
    language "C#"
    dotnetframework "net8.0-windows"
    targetdir "bin/%{cfg.buildcfg}/x64"
    objdir "bin-int/%{cfg.buildcfg}/x64"
    location "gamelauncherdave"

    files { "gamelauncherdave/**.cs" }
    files { "gamelauncherdave/assets/**" }
    
    -- Avoid generating duplicate AssemblyInfo attributes
    clr "Off"
    flags {"ShadowedVariables", "WPF"}
    linktimeoptimization "On"
    defines { "WINDOWS" }


    filter { "files:gamelauncherdave/assets/**" }
        buildaction "Resource"
    filter {}

    filter {}
