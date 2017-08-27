﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace WheresMyItems
{
	//draw problems > check position
	public class WheresMyItemsPlayer : ModPlayer
	{
		internal static bool[] waitingOnContents = new bool[1000];
		private const int itemSearchRange = 400;
		private int gameCounter;
		private Item[] curInv;
		private float sc = 0.8f;
		public static bool hover;

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (WheresMyItems.RandomBuffHotKey.JustPressed)
			{
				WheresMyItemsUI.visible = !WheresMyItemsUI.visible;
				if (WheresMyItemsUI.visible)
				{
					WheresMyItemsUI.box.SetText("");
					WheresMyItemsUI.box.Focus();
					Main.playerInventory = false;
				}
				// Since Main.blockInput, not called.
				//else
				//{
				//						WheresMyItemsUI.box.SetText("");
				//	WheresMyItemsUI.box.Unfocus();
				//}
			}
		}

		public bool ChestWithinRange(Chest c, int range)
		{
			Vector2 chestCenter = new Vector2((c.x * 16 + 16), (c.y * 16 + 16));
			return (chestCenter - player.Center).Length() < range;
		}

		public int TestForItem(Chest c, string searchTerm, ref Item[] nInv)
		{
			int found = 0;
			Item[] items = c.item;
			Item[] inv = new Item[3];
			for (int i = 0; i < 40; i++)
			{
				if (items[i] == null)
				{
					continue;
				}
				if (items[i].Name.ToLower().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1)
				{
					inv[found] = items[i].Clone();
					if (found > 0)
					{
						if (inv[found].type == inv[found - 1].type)
						{
							inv[found] = null;
							found--;
						}
					}
					found++;
					if (found == 3)
					{
						break;
					}
				}
			}
			nInv = inv;
			return found;
		}

		public void NewDustSlowed(Vector2 pos, int w, int h, int type, int interval)
		{
			Point tPos = pos.ToTileCoordinates();
			if (gameCounter % interval == 0)
			{
				int d = Dust.NewDust(pos, w, h, type, 0f, 0f, 0, Color.White, 0.9f);
			}
		}

		public Vector2 HalfSize(Texture2D t, float scale)
		{
			return new Vector2(t.Width * scale / 2, t.Height * scale / 2);
		}

		public Rectangle CreateRect(Vector2 v, Texture2D t)
		{
			return new Rectangle((int)v.X, (int)v.Y, t.Width, t.Height);
		}

		public DrawData[] DrawDataSlot(Vector2 cPos, Texture2D item, Texture2D box, float scale, Color colour)
		{
			DrawData[] d = new DrawData[2];
			d[0] = new DrawData(box, cPos - HalfSize(box, scale), CreateRect(Vector2.Zero, box), colour, 0f, Vector2.Zero, scale, SpriteEffects.None, 1);
			d[1] = new DrawData(item, cPos - HalfSize(item, scale), CreateRect(Vector2.Zero, item), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1);
			return d;
		}

		public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
		{
			if (WheresMyItemsUI.visible && player == Main.LocalPlayer)
			{
				gameCounter++;
				if (gameCounter == 99999)
				{
					gameCounter = 0;
				}
				WheresMyItemsUI.worldZoomDrawDatas.Clear();

				Texture2D[] bank = new Texture2D[3];
				Vector2[] pos = new Vector2[3];
				Texture2D box = mod.GetTexture("box");
				bank[0] = mod.GetTexture("b1");
				bank[1] = mod.GetTexture("b2");
				bank[2] = mod.GetTexture("b3");
				Vector2 plTopCenter = player.position + new Vector2(player.width / 2, 0f) - Main.screenPosition;
				pos[0] = plTopCenter + new Vector2(-48, -32);
				pos[1] = plTopCenter + new Vector2(0, -32);
				pos[2] = plTopCenter + new Vector2(48, -32);

				for (int i = 0; i < 3; i++)
				{
					WheresMyItemsUI.worldZoomDrawDatas.Add(DrawDataSlot(pos[i], bank[i], box, 1f, Color.White));
				}
				//Main.NewText(Main.player[Main.myPlayer].chest.ToString());
				/*if (player.townNPCs < 1f)
				{
					WheresMyItemsUI.box.SetText("");
					WheresMyItemsUI.box.Unfocus();
					//Main.NewText("Where's My Items search only available while near your town.");
					return;
				}*/
				string searchTerm = WheresMyItemsUI.SearchTerm;
				if (searchTerm.Length == 0) return;
				for (int chestIndex = 0; chestIndex < 1000; chestIndex++)
				{
					// If we are waiting on chest contents, skip.
					if (waitingOnContents[chestIndex])
					{
						continue;
					}
					Chest chest = Main.chest[chestIndex];
					if (chest != null && /*!Chest.IsPlayerInChest(i) &&*/ !Chest.isLocked(chest.x, chest.y))
					{
						if (ChestWithinRange(chest, itemSearchRange))
						{
							if (chest.item[0] == null)
							{
								var message = mod.GetPacket();
								message.Write((byte)MessageType.SilentRequestChestContents);
								message.Write(chestIndex);
								message.Send();
								waitingOnContents[chestIndex] = true;
								//Main.NewText($"Wait on {chestIndex}");
								continue;
							}

							// We could technically get item 0 but not item 39, so this check just makes sure we have all the items synced.
							//if (chest.item[chest.item.Length - 1] == null)
							//{
							//	// add 10 frames to wait time
							//	waitTimes[chestIndex] = 10;
							//	continue;
							//}
							int no = TestForItem(chest, searchTerm, ref curInv);
							if (no > 0)
							{
								NewDustSlowed(new Vector2(chest.x * 16, chest.y * 16), 32, 32, 16, 10); //107
																										// draw peek boxes

								Rectangle chestArea = new Rectangle(chest.x * 16, chest.y * 16, 32, 32);
								Vector2[] peekPos = new Vector2[3];
								Texture2D[] itemT = new Texture2D[3];
								if (hover)
								{
									Vector2 mousePosition = new Vector2(Main.mouseX, Main.mouseY) + Main.screenPosition;
									peekPos[1] = mousePosition - Main.screenPosition;

									// hover check
									if (!chestArea.Contains(mousePosition.ToPoint()))
									{
										continue;
									}
								}
								else
								{
									peekPos[1] = chestArea.Center.ToVector2() - Main.screenPosition;
								}
								//peekPos[1] += new Vector2(3, 4);
								// I'm not too sure why, but without this displacement, the peek box is slightly off center
								peekPos[0] = peekPos[1] - new Vector2(0, 48 * sc);
								peekPos[2] = peekPos[1] + new Vector2(0, 48 * sc);
								for (int i = 0; i < 3; i++)
								{
									peekPos[i] += new Vector2(0, sc * 24 * (3 - no));
									if (curInv[i] != null && !curInv[i].IsAir)
									{
										itemT[i] = Main.itemTexture[curInv[i].type];
										WheresMyItemsUI.worldZoomDrawDatas.Add(DrawDataSlot(peekPos[i], itemT[i], box, sc, Color.Red));
									}
								}
							}
						}
					}
				}
				// deal with extra invens
				Chest bk;
				for (int i = 0; i < 3; i++)
				{
					switch (i)
					{
						case 1:
							bk = player.bank2;
							break;

						case 2:
							bk = player.bank3;
							break;

						default:
							bk = player.bank;
							break;
					}
					if (TestForItem(bk, searchTerm, ref curInv) > 0)
					{
						NewDustSlowed(pos[i] + Main.screenPosition, 1, 1, 16, 30);
						pos[i].X -= 16;
						pos[i].Y -= 16;
						Vector2 hoverCorner = pos[i] + Main.screenPosition;
						Rectangle chestArea = new Rectangle((int)hoverCorner.X, (int)hoverCorner.Y, 32, 32);
						Vector2[] peekPos = new Vector2[3];
						Texture2D[] itemT = new Texture2D[3];
						if (hover)
						{
							// TODO, scale correctly
							Vector2 mousePosition = new Vector2(Main.mouseX, Main.mouseY) + Main.screenPosition;
							peekPos[0] = mousePosition - Main.screenPosition;
							//peekPos[0].X -= 16;
							if (!chestArea.Contains(mousePosition.ToPoint()))
							{
								continue;
							}
						}
						else
						{
							peekPos[0] = chestArea.Center.ToVector2() - Main.screenPosition;
							//peekPos[0].X -= 16;
						}
						peekPos[1] = peekPos[0] - new Vector2(0, 48 * sc);
						peekPos[2] = peekPos[1] - new Vector2(0, 48 * sc);
						for (int j = 0; j < 3; j++)
						{
							//peekPos[j] += new Vector2(0, sc * 24);
							if (curInv[j] != null && !curInv[j].IsAir)
							{
								itemT[j] = Main.itemTexture[curInv[j].type];
								WheresMyItemsUI.worldZoomDrawDatas.Add(DrawDataSlot(peekPos[j], itemT[j], box, sc, Color.Red));
							}
						}
					}
				}
			}
		}
	}
}