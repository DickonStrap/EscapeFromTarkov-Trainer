﻿using EFT.Trainer.Configuration;
using EFT.Trainer.Extensions;
using EFT.Trainer.UI;
using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace EFT.Trainer.Features
{
	[UsedImplicitly]
	internal class Radar : ToggleFeature
	{
		public override string Name => "radar";

		public override bool Enabled { get; set; } = false;

		[ConfigurationProperty(Order = 10)]
		public float RadarPercentage { get; set; } = 10f;

		[ConfigurationProperty(Order = 20)]
		public float RadarRange { get; set; } = 100f;

		[ConfigurationProperty(Order = 30)]
		public Color RadarBackground { get; set; } = new Color(0f, 0f, 0f, 0.5f);

		[ConfigurationProperty(Order = 30)]
		public Color RadarCrosshair { get; set; } = new Color(1f, 1f, 1f, 0.5f);

		[ConfigurationProperty(Order = 40)] 
		public bool ShowPlayers { get; set; } = true;

		[ConfigurationProperty(Order = 50)]
		public bool ShowScavs { get; set; } = true;

		[ConfigurationProperty(Order = 60)]
		public bool ShowScavRaiders { get; set; } = true;

		[ConfigurationProperty(Order = 70)]
		public bool ShowBosses { get; set; } = true;

		[ConfigurationProperty(Order = 80)]
		public bool ShowCultists { get; set; } = true;

		[UsedImplicitly]
		protected void OnGUI()
		{
			if (!Enabled)
				return;

			if (RadarRange <= 0)
				return;

			var screenPercentage = RadarPercentage / 100f;
			if (screenPercentage > 1)
				screenPercentage = 1;

			var hostiles = GameState.Current?.Hostiles;
			if (hostiles == null)
				return;

			var player = GameState.Current?.LocalPlayer;
			if (player == null)
				return;

			var camera = GameState.Current?.Camera;
			if (camera == null)
				return;

			var feature = FeatureFactory.GetFeature<Players>();
			if (feature == null)
				return;

			var radarSize = Mathf.Sqrt(Screen.height * Screen.width * screenPercentage) / 2;
			var radarX = Screen.width - radarSize;
			var radarY = Screen.height - radarSize;



			foreach (var enemy in hostiles)
			{
				if (!enemy.IsValid())
					continue;

				var position = enemy.Transform.position;

				var distance = Mathf.Round(Vector3.Distance(camera.transform.position, position));
				if (RadarRange > 0 && distance > RadarRange)
					continue;

				var hostileType = GetHostileType(enemy);

				if (hostileType == 0 && !ShowScavs)
					continue;

				if (hostileType == 1 && !ShowScavRaiders)
					continue;

				if (hostileType == 2 && !ShowCultists)
					continue;

				if (hostileType == 3 && !ShowBosses)
					continue;

				if ((hostileType == 4 || hostileType == 5) && !ShowPlayers)
					continue;

				var playerColor = hostileType switch
				{
					0 => feature.ScavColors.Color,
					1 => feature.ScavRaiderColors.Color,
					2 => feature.CultistColors.Color,
					3 => feature.BossColors.Color,
					4 => feature.BearColors.Color,
					5 => feature.UsecColors.Color,
					_ => feature.ScavColors.Color,
				};
				
				DrawRadarEnemy(player, enemy, radarSize, playerColor);

				}

			Render.DrawCrosshair(new Vector2(radarX + (radarSize / 2), radarY + (radarSize / 2)), radarSize / 2, RadarCrosshair, 2f);
			Render.DrawBox(radarX, radarY, radarSize, radarSize, 2f, RadarBackground);
		}

		private int GetHostileType(Player player)
		{
			var info = player.Profile?.Info;
			if (info == null)
				return 0;

			var settings = info.Settings;
			if (settings != null)
			{
				switch (settings.Role)
				{
					case WildSpawnType.pmcBot:
						return 1;
					case WildSpawnType.sectantWarrior:
						return 2;
				}

				if (settings.IsBoss())
					return 3;
			}

			return info.Side switch
			{
				EPlayerSide.Bear => 4,
				EPlayerSide.Usec => 5,
				_ => 0
			};
		}

		private void DrawRadarEnemy(Player player, Player enemy, float radarSize, Color playerColor)
		{
			var radarX = Screen.width - radarSize;
			var radarY = Screen.height - radarSize;

			var playerPosition = player.Transform.position;
			var enemyPosition = enemy.Transform.position;
			var playerEulerY = player.Transform.eulerAngles.y;

			var enemyRadar = FindRadarPoint(playerPosition, enemyPosition, playerEulerY, radarX, radarY, radarSize);

			var enemyOffset = enemyPosition + enemy.LookDirection * 8f;
			var enemyOffset2 = enemyPosition + (enemy.LookDirection * 4f) + (enemy.MovementContext.PlayerRealRight * 2f);
			var enemyOffset3 = enemyPosition + (enemy.LookDirection * 4f) - (enemy.MovementContext.PlayerRealRight * 2f);

			var enemyForward = FindRadarPoint(playerPosition, enemyOffset, playerEulerY, radarX, radarY, radarSize);
			var enemyArrow = FindRadarPoint(playerPosition, enemyOffset2, playerEulerY, radarX, radarY, radarSize);
			var enemyArrow2 = FindRadarPoint(playerPosition, enemyOffset3, playerEulerY, radarX, radarY, radarSize);

			Render.DrawLine(enemyRadar, enemyForward, 2f, Color.white);
			Render.DrawLine(enemyArrow, enemyForward, 2f, Color.white);
			Render.DrawLine(enemyArrow2, enemyForward, 2f, Color.white);
			Render.DrawCircle(enemyRadar, 10f, playerColor, 2f, 8);

		}

		private Vector2 FindRadarPoint(Vector3 playerPosition, Vector3 enemyPosition, float playerEulerY, float radarX, float radarY, float radarSize)
		{
			float enemyY = playerPosition.x - enemyPosition.x;
			float enemyX = playerPosition.z - enemyPosition.z;
			float enemyAtan = Mathf.Atan2(enemyY, enemyX) * Mathf.Rad2Deg - 270 - playerEulerY;

			var enemyDistance = Mathf.Round(Vector3.Distance(playerPosition, enemyPosition));

			float enemyRadarX = enemyDistance * Mathf.Cos(enemyAtan * Mathf.Deg2Rad);
			float enemyRadarY = enemyDistance * Mathf.Sin(enemyAtan * Mathf.Deg2Rad);

			enemyRadarX = enemyRadarX * (radarSize / RadarRange) / 2f;
			enemyRadarY = enemyRadarY * (radarSize / RadarRange) / 2f;

			return new Vector2(radarX + radarSize / 2f + enemyRadarX, radarY + radarSize / 2f + enemyRadarY);
		}
	}
}