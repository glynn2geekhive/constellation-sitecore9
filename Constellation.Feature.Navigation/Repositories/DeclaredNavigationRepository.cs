﻿using Constellation.Feature.Navigation.Models;
using Constellation.Foundation.Data;
using Constellation.Foundation.ModelMapping;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Constellation.Feature.Navigation.Repositories
{
	/// <summary>
	/// Given a Datasource that represents a Navigation Menu, will generate
	/// a tree of ViewModels for use in constructing HTML page navigation.
	/// Useful for static navigation such as primary site menus or footers.
	/// </summary>
	public class DeclaredNavigationRepository : IDeclaredNavigationRepository
	{
		#region Constructor
		public DeclaredNavigationRepository(IModelMapper modelMapper)
		{
			ModelMapper = modelMapper;
		}
		#endregion

		#region Properties
		protected IModelMapper ModelMapper { get; }
		#endregion

		/// <summary>
		/// Returns an instance of NavigationMenu with available link groups and navigation links
		/// pre-populated and ready with display-compatible values.
		/// </summary>
		/// <param name="datasource">The Navigation Menu Item to process.</param>
		/// <param name="contextItem">Optional. The Item represented by the current HttpRequest.</param>
		/// <returns></returns>
		public NavigationMenu GetNavigation(Item datasource, Item contextItem = null)
		{
			var output = ModelMapper.MapItemToNew<NavigationMenu>(datasource);

			ProcessGroupChildren(datasource, output, contextItem);

			return output;
		}

		private void ProcessGroupChildren(Item parent, LinkGroup parentGroup, Item contextItem)
		{
			var children = parent.GetChildren();

			foreach (Item child in children)
			{
				if (child.IsDerivedFrom(NavigationTemplateIDs.LinkGroupID))
				{
					var group = ModelMapper.MapItemToNew<LinkGroup>(child);
					parentGroup.ChildGroups.Add(group);
					ProcessGroupChildren(child, group, contextItem);
					continue;
				}

				var link = BuildNavigationLink(child, contextItem);
				if (link == null) continue;

				parentGroup.ChildLinks.Add(link);
				ProcessLinkChildren(child, link, contextItem);
			}
		}

		private void ProcessLinkChildren(Item parent, NavigationLink parentLink, Item contextItem)
		{
			var children = parent.GetChildren();

			foreach (Item child in children)
			{
				var link = BuildNavigationLink(child, contextItem);
				if (link == null) continue;
				parentLink.ChildLinks.Add(link);
				ProcessLinkChildren(child, link, contextItem);
			}
		}

		private NavigationLink BuildNavigationLink(Item child, Item contextItem)
		{
			if (!child.IsDerivedFrom(NavigationTemplateIDs.NavigationLinkID))
			{
				LogIncompatibleItemWarning(child);
				return null;
			}

			NavigationLink link = null;

			if (child.IsDerivedFrom(NavigationTemplateIDs.ImageNavigationLinkID))
			{
				link = ModelMapper.MapItemToNew<ImageNavigationLink>(child);
			}

			link = ModelMapper.MapItemToNew<NavigationLink>(child);

			if (contextItem == null)
			{
				return link;
			}

			if (contextItem.Axes.IsAncestorOf(child))
			{
				link.IsActive = true;
			}

			return link;
		}

		private static void LogIncompatibleItemWarning(Item item)
		{
			Log.Warn(
				$"Navigation Repository encountered an Item in navigation that does not descend from Link Group or Navigation Link. Item {item.Paths.FullPath} ignored.",
				typeof(DeclaredNavigationRepository));
		}
	}
}
