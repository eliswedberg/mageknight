'use client';

import React, { useState } from 'react';

interface MapTileProps {
  imageSrc: string;
  onSelect?: (area: string) => void;
}

const MapTile: React.FC<MapTileProps> = ({ imageSrc, onSelect }) => {
  const [selectedArea, setSelectedArea] = useState<string | null>(null);

  const handleAreaClick = (areaName: string) => {
    setSelectedArea(areaName);
    onSelect?.(areaName);
  };

  return (
    <div className="relative w-full h-full">
      {/* Background Image */}
      <img 
        src={imageSrc} 
        alt="Map Tile" 
        className="w-full h-full object-cover"
      />
      
      {/* SVG Overlay */}
      <svg 
        className="absolute inset-0 w-full h-full"
        viewBox="-311.769 -300 623.538 600"
        preserveAspectRatio="xMidYMid meet"
      >
        <defs>
          <style>{`
            .hex-hit { 
              cursor: pointer; 
              fill: transparent; 
            }
            .hex-overlay { 
              pointer-events: none; 
              fill: yellow; 
              opacity: 0; 
              transition: opacity 0.15s ease; 
            }
            .hex-group:hover .hex-overlay { 
              opacity: 0.35; 
            }
            .hex-group.selected .hex-overlay { 
              opacity: 0.5; 
              fill: deepskyblue; 
            }
          `}</style>
        </defs>

        {/* Top Left */}
        <g 
          className={`hex-group ${selectedArea === 'top-left' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('top-left')}
        >
          <polygon 
            className="hex-hit" 
            points="0,-240 0,-120 -103.9,-60 -207.8,-120 -207.8,-240 -103.9,-300"
          />
          <polygon 
            className="hex-overlay" 
            points="0,-240 0,-120 -103.9,-60 -207.8,-120 -207.8,-240 -103.9,-300"
          />
        </g>

        {/* Top Right */}
        <g 
          className={`hex-group ${selectedArea === 'top-right' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('top-right')}
        >
          <polygon 
            className="hex-hit" 
            points="207.8,-240 207.8,-120 103.9,-60 0,-120 0,-240 103.9,-300"
          />
          <polygon 
            className="hex-overlay" 
            points="207.8,-240 207.8,-120 103.9,-60 0,-120 0,-240 103.9,-300"
          />
        </g>

        {/* Mid Left */}
        <g 
          className={`hex-group ${selectedArea === 'mid-left' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('mid-left')}
        >
          <polygon 
            className="hex-hit" 
            points="-103.9,-60 -103.9,60 -207.8,120 -311.8,60 -311.8,-60 -207.8,-120"
          />
          <polygon 
            className="hex-overlay" 
            points="-103.9,-60 -103.9,60 -207.8,120 -311.8,60 -311.8,-60 -207.8,-120"
          />
        </g>

        {/* Mid Center */}
        <g 
          className={`hex-group ${selectedArea === 'mid-center' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('mid-center')}
        >
          <polygon 
            className="hex-hit" 
            points="103.9,-60 103.9,60 0,120 -103.9,60 -103.9,-60 0,-120"
          />
          <polygon 
            className="hex-overlay" 
            points="103.9,-60 103.9,60 0,120 -103.9,60 -103.9,-60 0,-120"
          />
        </g>

        {/* Mid Right */}
        <g 
          className={`hex-group ${selectedArea === 'mid-right' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('mid-right')}
        >
          <polygon 
            className="hex-hit" 
            points="311.8,-60 311.8,60 207.8,120 103.9,60 103.9,-60 207.8,-120"
          />
          <polygon 
            className="hex-overlay" 
            points="311.8,-60 311.8,60 207.8,120 103.9,60 103.9,-60 207.8,-120"
          />
        </g>

        {/* Bottom Left */}
        <g 
          className={`hex-group ${selectedArea === 'bottom-left' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('bottom-left')}
        >
          <polygon 
            className="hex-hit" 
            points="0,120 0,240 -103.9,300 -207.8,240 -207.8,120 -103.9,60"
          />
          <polygon 
            className="hex-overlay" 
            points="0,120 0,240 -103.9,300 -207.8,240 -207.8,120 -103.9,60"
          />
        </g>

        {/* Bottom Right */}
        <g 
          className={`hex-group ${selectedArea === 'bottom-right' ? 'selected' : ''}`}
          onClick={() => handleAreaClick('bottom-right')}
        >
          <polygon 
            className="hex-hit" 
            points="207.8,120 207.8,240 103.9,300 0,240 0,120 103.9,60"
          />
          <polygon 
            className="hex-overlay" 
            points="207.8,120 207.8,240 103.9,300 0,240 0,120 103.9,60"
          />
        </g>
      </svg>
    </div>
  );
};

export default MapTile;
