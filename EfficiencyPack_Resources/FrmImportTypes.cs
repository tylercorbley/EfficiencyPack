using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EfficiencyPack
{
    public partial class FrmImportTypes : Form
    {
        private List<(string family, string type, string systemFamily)> typeData;
        private List<(string family, string type, string systemFamily)> selectedTypes = new List<(string family, string type, string systemFamily)>();
        private TreeView treeViewTypes; // Add a TreeView control
        private TextBox txtSearch;

        public FrmImportTypes(List<(string family, string type, string systemFamily)> typeData)
        {
            InitializeComponent();
            this.typeData = typeData;

            InitializeControls();
            PopulateTreeView();
        }

        private void InitializeControls()
        {
            // Initialize TreeView
            this.treeViewTypes = new TreeView();
            this.treeViewTypes.Width = 400;
            this.treeViewTypes.Height = 675;
            this.treeViewTypes.Location = new Point(15, 54);
            this.treeViewTypes.CheckBoxes = true;

            // Add event handler for AfterCheck event to handle checkbox changes
            this.treeViewTypes.AfterCheck += TreeViewTypes_AfterCheck;

            // Add TreeView control to the form
            this.Controls.Add(treeViewTypes);

            // Initialize TextBox for search
            this.txtSearch = new TextBox();
            this.txtSearch.Width = 344;
            this.txtSearch.Height = 20;
            this.txtSearch.Location = new Point(71, 28);
            this.txtSearch.TextChanged += TxtSearch_TextChanged;

            // Add TextBox control to the form
            this.Controls.Add(txtSearch);
        }


        private void PopulateTreeView()
        {
            treeViewTypes.Nodes.Clear();

            foreach (var entry in typeData)
            {
                string family = entry.family;
                string type = entry.type;
                string systemFamily = entry.systemFamily;

                var familyNode = FindOrCreateNode(treeViewTypes.Nodes, family);
                familyNode.Tag = (family, null as string, null as string); // Set tag for family node
                var systemFamilyNode = FindOrCreateNode(familyNode.Nodes, systemFamily);
                systemFamilyNode.Tag = (family, null as string, systemFamily); // Set tag for system family node
                var typeNode = systemFamilyNode.Nodes.Add(type);
                typeNode.Tag = (family, type, systemFamily); // Set tag for type node
            }

            treeViewTypes.CollapseAll();
        }

        private TreeNode FindOrCreateNode(TreeNodeCollection nodes, string nodeName)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text == nodeName)
                {
                    return node;
                }
            }

            // If the node doesn't exist, create and return a new one
            return nodes.Add(nodeName, nodeName);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            selectedTypes.Clear(); // Clear previously selected types before collecting

            // Collect selected items
            foreach (TreeNode node in treeViewTypes.Nodes)
            {
                CollectSelectedNodes(node);
            }

            // Debugging: Log selected types count
            Console.WriteLine($"Selected types count: {selectedTypes.Count}");

            if (selectedTypes.Count == 0)
            {
                MessageBox.Show("No types selected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CollectSelectedNodes(TreeNode parentNode)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Checked && node.Tag != null)
                {
                    var tuple = (ValueTuple<string, string, string>)node.Tag;

                    // Only add to selectedTypes if none of the tuple elements are null
                    if (!string.IsNullOrEmpty(tuple.Item1) && !string.IsNullOrEmpty(tuple.Item2) && !string.IsNullOrEmpty(tuple.Item3))
                    {
                        selectedTypes.Add(tuple);

                        // Debugging: Log the selected node's details
                        Console.WriteLine($"Selected node: {tuple.Item1}, {tuple.Item2}, {tuple.Item3}");
                    }
                }

                // Recursively collect selected nodes
                CollectSelectedNodes(node);
            }
        }

        private void TreeViewTypes_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Update child nodes when a parent node is checked
            if (e.Node.Nodes.Count > 0)
            {
                foreach (TreeNode childNode in e.Node.Nodes)
                {
                    childNode.Checked = e.Node.Checked;
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text.Trim().ToLower();

            foreach (TreeNode node in treeViewTypes.Nodes)
            {
                FilterNodesBySearch(node, searchTerm);
            }

            // Reset font styles if the search term is empty
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ResetFontStyles(treeViewTypes.Nodes);
            }
        }

        private void FilterNodesBySearch(TreeNode node, string searchTerm)
        {
            // If the node is null, return
            if (node == null)
            {
                return;
            }

            foreach (TreeNode childNode in node.Nodes)
            {
                // Check if the node's text contains the search term
                bool nodeMatchesSearch = childNode.Text.ToLower().Contains(searchTerm.ToLower());

                // Set the font style of the node based on the search term
                childNode.ForeColor = nodeMatchesSearch ? treeViewTypes.ForeColor : Color.Gray;
                childNode.NodeFont = nodeMatchesSearch ? new Font(treeViewTypes.Font, FontStyle.Bold) : new Font(treeViewTypes.Font, FontStyle.Regular);

                // Recursively filter child nodes
                FilterNodesBySearch(childNode, searchTerm);
            }
        }

        private void ResetFontStyles(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.NodeFont = new Font(treeViewTypes.Font, FontStyle.Regular); // Reset font style
                node.ForeColor = treeViewTypes.ForeColor; // Reset fore color

                // Recursively reset font styles for child nodes
                ResetFontStyles(node.Nodes);
            }
        }

        public List<(string, string, string)> GetSelectedTypes()
        {
            return selectedTypes;
        }
    }
}
