type MapRover = { id: string; identifier: string; position: { x: number; y: number } };
export type RoverMarker = { x: number; y: number; anchorX: number; anchorY: number; displaced: boolean };

// Display coordinates only. Never change the persisted route or rover location.
// Reserve room for both the icon and its identifier/progress labels.
export function layoutRovers(rovers: MapRover[]): Map<string, RoverMarker> {
  const placed = new Map<string, RoverMarker>();
  for (const rover of [...rovers].sort((a,b)=>a.identifier.localeCompare(b.identifier))) {
    const anchorX = rover.position.x * 10, anchorY = rover.position.y * 6.4;
    const candidates: {x:number; y:number; distance:number}[] = [];
    for (let row=-7; row<=7; row++) for (let col=-13; col<=13; col++) {
      const x=anchorX+col*76, y=anchorY+row*88;
      if (x>=42 && x<=958 && y>=105 && y<=585)
        candidates.push({x,y,distance:col*col*76*76+row*row*88*88});
    }
    candidates.sort((a,b)=>a.distance-b.distance || a.y-b.y || a.x-b.x);
    const spot=candidates.find(p=>[...placed.values()].every(q=>Math.abs(p.x-q.x)>=72 || Math.abs(p.y-q.y)>=84));
    if (!spot) throw new Error('Too many rovers to display on the warehouse map.');
    placed.set(rover.id,{x:spot.x,y:spot.y,anchorX,anchorY,displaced:spot.x!==anchorX || spot.y!==anchorY});
  }
  return placed;
}
