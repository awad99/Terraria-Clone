using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SimpleTerrariaClone
{
    public class Camera2D
    {
        private Matrix _transform;
        private Vector2 _position;
        private float _zoom;
        private readonly Viewport _viewport;

        // Target following properties
        private Vector2 _target;
        private bool _isFollowing;
        private float _followSpeed = 5.0f;
        private bool _snapToPixel = true;

        // Camera position offset
        private Vector2 _cameraOffset = new Vector2(-400f, 200f); // X offset of 150px left, Y offset of 200px down

        public Matrix Transform => _transform;
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }
        public float Zoom
        {
            get { return _zoom; }
            set { _zoom = MathHelper.Clamp(value, 0.1f, 10f); }
        }

        // Property to adjust camera offset
        public Vector2 CameraOffset
        {
            get { return _cameraOffset; }
            set { _cameraOffset = value; }
        }

        public Camera2D(Viewport viewport)
        {
            _viewport = viewport;
            _position = new Vector2(0, 0);
            _zoom = 1.0f;
            _isFollowing = false;

            // Initialize transform matrix
            UpdateTransform();
        }

        public void FollowTarget(Vector2 target)
        {
            // Apply camera offset to the target position
            _target = target + _cameraOffset;
            _isFollowing = true;
        }

        public void StopFollowing()
        {
            _isFollowing = false;
        }

        public void Update(GameTime gameTime = null)
        {
            if (_isFollowing && gameTime != null)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Smooth follow with lerp
                _position = Vector2.Lerp(_position, _target, _followSpeed * dt);

                // Calculate screen center offset
                Vector2 offset = new Vector2(_viewport.Width * 0.5f, _viewport.Height * 0.5f) / _zoom;

                // Apply offset and calculate final position
                Vector2 finalPosition = _position - offset;

                // Snap to pixel grid if enabled
                if (_snapToPixel)
                {
                    _position = new Vector2(
                        (float)Math.Round(finalPosition.X),
                        (float)Math.Round(finalPosition.Y)
                    ) + offset;
                }
            }

            // Update the transformation matrix
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            // Calculate screen center offset
            Vector2 offset = new Vector2(_viewport.Width * 0.5f, _viewport.Height * 0.5f);

            // Create translation matrix
            Matrix translationMatrix = Matrix.CreateTranslation(new Vector3(
                -_position.X,
                -_position.Y,
                0));

            // Create scale matrix
            Matrix scaleMatrix = Matrix.CreateScale(_zoom, _zoom, 1);

            // Create translation matrix for center offset
            Matrix offsetMatrix = Matrix.CreateTranslation(new Vector3(
                offset.X,
                offset.Y,
                0));

            // Combine matrices to create final transform
            _transform = translationMatrix * scaleMatrix * offsetMatrix;
        }

        // Calculate world position from screen coordinates
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Matrix invertedMatrix = Matrix.Invert(_transform);
            return Vector2.Transform(screenPosition, invertedMatrix);
        }

        // Calculate screen position from world coordinates
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, _transform);
        }

        // Get visible area in world space
        public Rectangle GetVisibleArea()
        {
            Vector2 min = ScreenToWorld(Vector2.Zero);
            Vector2 max = ScreenToWorld(new Vector2(_viewport.Width, _viewport.Height));

            return new Rectangle(
                (int)min.X,
                (int)min.Y,
                (int)(max.X - min.X),
                (int)(max.Y - min.Y)
            );
        }
    }
}