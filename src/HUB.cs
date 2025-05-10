using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleTerrariaClone
{
    public class InventorySlot
    {
        public Texture2D ItemTexture { get; set; }
        public bool IsEmpty => ItemTexture == null;
        public Rectangle Bounds { get; set; }
        public bool IsSelected { get; set; }

        public InventorySlot(Rectangle bounds)
        {
            Bounds = bounds;
            ItemTexture = null;
            IsSelected = false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D slotTexture)
        {
            // Draw the slot background with different color if selected
            Color slotColor = IsSelected ? Color.Yellow : Color.White;
            spriteBatch.Draw(slotTexture, Bounds, slotColor);

            // Draw the item if there is one
            if (!IsEmpty)
            {
                // Calculate position to center item in slot
                int itemSize = Math.Min(Bounds.Width, Bounds.Height) - 4;
                int x = Bounds.X + (Bounds.Width - itemSize) / 2;
                int y = Bounds.Y + (Bounds.Height - itemSize) / 2;

                spriteBatch.Draw(ItemTexture, new Rectangle(x, y, itemSize, itemSize), Color.White);
            }
        }
    }

    public class HUD
    {
        private List<InventorySlot> inventorySlots;
        private Texture2D slotTexture;
        private int slotSize = 40;
        private int slotPadding = 4;
        private int slotMargin = 10;
        private int selectedSlotIndex = 0;

        public HUD(Texture2D slotTexture, int numberOfSlots)
        {
            this.slotTexture = slotTexture;
            inventorySlots = new List<InventorySlot>();

            // Create inventory slots
            for (int i = 0; i < numberOfSlots; i++)
            {
                int x = slotMargin + i * (slotSize + slotPadding);
                int y = slotMargin;
                InventorySlot slot = new InventorySlot(new Rectangle(x, y, slotSize, slotSize));

                // Set the first slot as selected by default
                slot.IsSelected = (i == 0);

                inventorySlots.Add(slot);
            }
        }

        public void SetItem(int slotIndex, Texture2D itemTexture)
        {
            if (slotIndex >= 0 && slotIndex < inventorySlots.Count)
            {
                inventorySlots[slotIndex].ItemTexture = itemTexture;
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < inventorySlots.Count)
            {
                // Deselect the currently selected slot
                if (selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Count)
                {
                    inventorySlots[selectedSlotIndex].IsSelected = false;
                }

                // Select the new slot
                inventorySlots[slotIndex].IsSelected = true;
                selectedSlotIndex = slotIndex;
            }
        }

        public int GetSelectedSlotIndex()
        {
            return selectedSlotIndex;
        }

        public Texture2D GetSelectedItemTexture()
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Count)
            {
                return inventorySlots[selectedSlotIndex].ItemTexture;
            }
            return null;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var slot in inventorySlots)
            {
                slot.Draw(spriteBatch, slotTexture);
            }
        }
    }
}