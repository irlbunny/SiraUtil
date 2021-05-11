﻿using HarmonyLib;
using IPA;
using IPA.Loader;
using SiraUtil.Affinity;
using SiraUtil.Installers;
using SiraUtil.Tools.FPFC;
using SiraUtil.Zenject;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace SiraUtil
{
    [Plugin(RuntimeOptions.DynamicInit)]
    internal class Plugin
    {
        private readonly Harmony _harmony;
        private readonly ZenjectManager _zenjectManager;
        public static IPALogger Log { get; private set; } = null!;
        public const string ID = "dev.auros.sirautil";

        [Init]
        public Plugin(IPALogger logger, PluginMetadata metadata)
        {
            Log = logger;
            _harmony = new Harmony(ID);
            _zenjectManager = new ZenjectManager();

            // Adds the Zenjector type to BSIPA's Init Injection system so mods can receive it in their [Init] parameters.
            PluginInitInjector.AddInjector(typeof(Zenjector), ConstructZenjector);

            Zenjector zenjector = (ConstructZenjector(null!, null!, metadata) as Zenjector)!;
            zenjector.Install(Location.App, (Container) =>
            {
                Container.Bind<AffinityManager>().ToSelf().AsSingle().CopyIntoAllSubContainers();
            });
            zenjector.Install<FPFCInstaller>(Location.Menu | Location.Player);
            zenjector.Install<SiraPlayerInstaller>(Location.Player);
            zenjector.Install<SiraSettingsInstaller>(Location.App);
            zenjector.Install<SiraMenuInstaller>(Location.Menu);
        }

        [OnEnable]
        public void OnEnable()
        {
            _harmony.PatchAll();
            _zenjectManager.Enable();
        }

        [OnDisable]
        public void OnDisable()
        {
            _zenjectManager.Disable();
            _harmony.UnpatchAll(ID);
        }

        private object ConstructZenjector(object previous, ParameterInfo _, PluginMetadata meta)
        {
            if (previous is not null)
                return previous;

            Zenjector zenjector = new(meta);
            _zenjectManager.Add(zenjector);
            return zenjector;
        }
    }
}