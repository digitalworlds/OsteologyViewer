using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;

// Workaround for Unity 6.3 on Linux not auto-refreshing assets
// Put this in an Editor folder
// Uses FileSystemWatcher to detect changes
// Forces an AssetDatabase.Refresh once play mode is inactive
[InitializeOnLoad]
public static class AutoRefreshWorkaround
{
	static FileSystemWatcher fsWatcher;
	static bool isDirty;
	static double lastChangeTime;

	static AutoRefreshWorkaround()
	{
		if (EditorPrefs.GetBool("kAutoRefresh", true) == false)
		{
			// Respect Unity's own auto-refresh setting — if the user has it
			// disabled intentionally, don't override that.
			return;
		}

		string assetsPath = Path.GetFullPath("Assets");

		fsWatcher = new FileSystemWatcher(assetsPath)
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.LastWrite
						 | NotifyFilters.FileName
						 | NotifyFilters.DirectoryName
						 | NotifyFilters.CreationTime,
			EnableRaisingEvents = true
		};

		fsWatcher.Changed += OnFileEvent;
		fsWatcher.Created += OnFileEvent;
		fsWatcher.Deleted += OnFileEvent;
		fsWatcher.Renamed += (_, e) => MarkDirty();

		EditorApplication.update += OnEditorUpdate;
		AssemblyReloadEvents.beforeAssemblyReload += Dispose;

		Debug.Log("[AutoRefreshWorkaround] Active — watching Assets/ for changes.");
	}

	static void OnFileEvent(object sender, FileSystemEventArgs e)
	{
		// Ignore .meta and hidden/temp files
		if (e.FullPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
			return;
		if (Path.GetFileName(e.FullPath).StartsWith("."))
			return;

		MarkDirty();
	}

	static void MarkDirty()
	{
		isDirty = true;
		lastChangeTime = EditorApplication.timeSinceStartup;
	}

	static void OnEditorUpdate()
	{
		if (!isDirty)
			return;

		// Don't refresh while in play mode
		if (EditorApplication.isPlaying || EditorApplication.isCompiling)
			return;

		// Only refresh while Unity Editor is the active application window.
		// Prevents focus stealing while editing in an external IDE.
		if (!InternalEditorUtility.isApplicationActive)
			return;

		// Wait until changes have settled
		const double WAIT_SECONDS = 1.0;
		if (EditorApplication.timeSinceStartup - lastChangeTime < WAIT_SECONDS)
			return;

		isDirty = false;

		Debug.Log("[AutoRefreshWorkaround] File changes detected — forcing AssetDatabase.Refresh.");
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
	}

	static void Dispose()
	{
		if (fsWatcher != null)
		{
			fsWatcher.EnableRaisingEvents = false;
			fsWatcher.Dispose();
			fsWatcher = null;
		}
	}
}