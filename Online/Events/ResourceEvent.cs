﻿using Steamworks;
using System;

namespace RainMeadow
{
    public abstract class ResourceEvent : PlayerEvent
    {
        public override long EstimatedSize => base.EstimatedSize + onlineResource.SizeOfIdentifier();
        public OnlineResource onlineResource;

        protected ResourceEvent() { }
        public ResourceEvent(OnlineResource onlineResource)
        {
            this.onlineResource = onlineResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref onlineResource);
        }
    }
}