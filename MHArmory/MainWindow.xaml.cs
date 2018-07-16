﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MHArmory.Core;
using MHArmory.Core.DataStructures;
using MHArmory.ViewModels;

namespace MHArmory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly RootViewModel rootViewModel = new RootViewModel();

        private SkillSelectorWindow skillSelectorWindow;

        public MainWindow()
        {
            InitializeComponent();

            CommandBindings.Add(RoutedCommands.CreateCommandBinding(RoutedCommands.OpenSkillsSelector, OpenSkillSelector));

            AssemblyName asmName = Assembly.GetEntryAssembly().GetName();
            Title = $"{asmName.Name} {asmName.Version.Major}.{asmName.Version.Minor}.{asmName.Version.Build}";

            DataContext = rootViewModel;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Dispatcher.Yield(DispatcherPriority.Render);

            skillSelectorWindow = new SkillSelectorWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var source = new MhwDbDataSource.DataSource(null);

            ISkill[] skills = await source.GetSkills();
            IArmorPiece[] armors = await source.GetArmorPieces();
            ICharm[] charms = await source.GetCharms();
            IJewel[] jewels = await source.GetJewels();

            if (skills == null)
            {
                CloseApplicationBecauseOfDataSource(((ISkillDataSource)source).Description);
                return;
            }
            else if (armors == null)
            {
                CloseApplicationBecauseOfDataSource(((IArmorDataSource)source).Description);
                return;
            }
            else if (charms == null)
            {
                CloseApplicationBecauseOfDataSource(((ICharmDataSource)source).Description);
                return;
            }
            else if (jewels == null)
            {
                CloseApplicationBecauseOfDataSource(((IJewelDataSource)source).Description);
                return;
            }

            IList<SkillViewModel> allSkills = skills
                .OrderBy(x => x.Name)
                .Select(x => new SkillViewModel(x, jewels.Where(j => j.Abilities.Any(a => a.Skill.Id == x.Id)).ToList(), rootViewModel, skillSelectorWindow.SkillSelector))
                .ToList();

            IList<AbilityViewModel> allAbilities = allSkills
                .SelectMany(x => x.Abilities)
                .ToList();

            rootViewModel.SelectedAbilities = allAbilities;

            GlobalData.Instance.SetSkills(allSkills);
            GlobalData.Instance.SetAbilities(allAbilities);

            GlobalData.Instance.SetArmors(armors);

            var skillsToArmorsMap = new Dictionary<int, IList<IArmorPiece>>();
            var skillsToCharmsMap = new Dictionary<int, IList<ICharm>>();
            var skillsToJewelsMap = new Dictionary<int, IList<IJewel>>();

            foreach (ISkill skill in skills)
            {
                skillsToArmorsMap.Add(skill.Id, armors
                    .Where(x => x.Abilities.Any(a => a.Skill.Id == skill.Id))
                    .ToList()
                );

                skillsToCharmsMap.Add(skill.Id, charms
                    .Where(x => x.Levels.Any(l => l.Abilities.Any(a => a.Skill.Id == skill.Id)))
                    .ToList()
                );

                skillsToJewelsMap.Add(skill.Id, jewels
                    .Where(x => x.Abilities.Any(a => a.Skill.Id == skill.Id))
                    .ToList()
                );
            }

            GlobalData.Instance.SetSkillsToArmorsMap(skillsToArmorsMap);
            GlobalData.Instance.SetSkillsToCharmsMap(skillsToCharmsMap);
            GlobalData.Instance.SetSkillsToJewelsMap(skillsToJewelsMap);

            rootViewModel.FoundArmorSets = new ArmorSetViewModel[]
            {
                new ArmorSetViewModel { ArmorPieces = armors.Skip(armors.Length - 10).Take(5).ToList() },
                new ArmorSetViewModel { ArmorPieces = armors.Skip(armors.Length - 5).ToList() }
            };

            rootViewModel.IsDataLoading = false;
            rootViewModel.IsDataLoaded = true;
        }

        private void OpenSkillSelector(object parameter)
        {
            skillSelectorWindow.Show();
        }

        private void CloseApplicationBecauseOfDataSource(string description)
        {
            string message = $"Could not load required data from '{description}'\nContact the data source owner for more information.";
            MessageBox.Show(this, message, "Data source error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            skillSelectorWindow.ApplicationClose();
        }
    }
}
