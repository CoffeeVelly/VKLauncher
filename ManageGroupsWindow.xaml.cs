using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VKLauncher.Models;

namespace VKLauncher
{
    public partial class ManageGroupsWindow : Window
    {
        private List<ServiceGroup> groups;
        private List<string> allServices;
        private ServiceGroup? selectedGroup;

        public ManageGroupsWindow(List<ServiceGroup> existingGroups, List<string> services)
        {
            InitializeComponent();
            groups = existingGroups;
            allServices = services;

            GroupListBox.ItemsSource = groups;
        }

        public List<ServiceGroup> UpdatedGroups => groups;

        private void GroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupListBox.SelectedItem is ServiceGroup group)
            {
                selectedGroup = group;
                GroupNameBox.Text = group.GroupName;

                RefreshServiceLists();
            }
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var newGroup = new ServiceGroup { GroupName = "新服务组", ServiceNames = new List<string>() };
            groups.Add(newGroup);
            GroupListBox.Items.Refresh();
            GroupListBox.SelectedItem = newGroup;
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGroup != null)
            {
                groups.Remove(selectedGroup);
                GroupListBox.Items.Refresh();
                selectedGroup = null;
                GroupNameBox.Text = "";
                AvailableListBox.ItemsSource = null;
                SelectedListBox.ItemsSource = null;
            }
        }

        private void SaveGroup_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGroup == null) return;

            selectedGroup.GroupName = GroupNameBox.Text;
            MessageBox.Show("保存成功！");
            GroupListBox.Items.Refresh();
        }

        private void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGroup != null)
            {
                selectedGroup.GroupName = GroupNameBox.Text;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void MoveRight_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGroup == null) return;

            var selectedItems = AvailableListBox.SelectedItems.Cast<string>().ToList();
            foreach (var item in selectedItems)
            {
                if (!selectedGroup.ServiceNames.Contains(item))
                    selectedGroup.ServiceNames.Add(item);
            }

            RefreshServiceLists();
        }

        private void MoveLeft_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGroup == null) return;

            var selectedItems = SelectedListBox.SelectedItems.Cast<string>().ToList();
            foreach (var item in selectedItems)
            {
                selectedGroup.ServiceNames.Remove(item);
            }

            RefreshServiceLists();
        }

        private void RefreshServiceLists()
        {
            if (selectedGroup == null) return;

            var selected = selectedGroup.ServiceNames;
            var available = allServices.Except(selected).ToList();

            AvailableListBox.ItemsSource = null;
            AvailableListBox.ItemsSource = available;

            SelectedListBox.ItemsSource = null;
            SelectedListBox.ItemsSource = new List<string>(selected);
        }
    }
}
