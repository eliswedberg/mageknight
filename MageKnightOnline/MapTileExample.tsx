import React from 'react';
import MapTile from './MapTile';

const MapTileExample: React.FC = () => {
  const handleAreaSelect = (area: string) => {
    console.log('Selected area:', area);
    // You can add additional logic here, like updating state or making API calls
  };

  return (
    <div className="w-96 h-96 border border-gray-300 rounded-lg overflow-hidden">
      <MapTile 
        imageSrc="/tiles/tile1.png" 
        onSelect={handleAreaSelect} 
      />
    </div>
  );
};

export default MapTileExample;
