"""Offline source/data integrity and static route-clearance check; not Unity execution."""
from pathlib import Path
import json
import math
import re
import yaml

ROOT = Path(__file__).resolve().parents[1]

def position(node):
    p = node['position']
    return float(p['x']), float(p['z'])

def clear_against_waiting(route, waiting):
    # Independent geometric check of the authored solution, with every prior car exited.
    # Car bounds are width .85, length 1.4; all Phase 1 segments are cardinal.
    for start, end in zip(route, route[1:]):
        dx, dz = end[0] - start[0], end[1] - start[1]
        assert dx == 0 or dz == 0, 'Phase 1 clearance validator expects cardinal roads'
        half = (.7, .425) if dx else (.425, .7)
        for point, other_half in waiting:
            # Segment against Minkowski-expanded waiting-car bounds, using slab intervals.
            lower, upper = 0.0, 1.0
            for axis, direction in enumerate((dx, dz)):
                radius = half[axis] + other_half[axis]
                lo, hi = point[axis] - radius, point[axis] + radius
                if direction == 0:
                    if start[axis] <= lo or start[axis] >= hi:
                        lower, upper = 1, 0
                        break
                else:
                    a, b = (lo - start[axis]) / direction, (hi - start[axis]) / direction
                    lower, upper = max(lower, min(a, b)), min(upper, max(a, b))
            assert lower >= upper, 'Intended route intersects a waiting car'

manifest = json.loads((ROOT / 'Packages/manifest.json').read_text())
for package in ['com.unity.render-pipelines.universal', 'com.unity.inputsystem', 'com.unity.test-framework', 'com.unity.ugui']:
    assert package in manifest['dependencies']
assert '6000.3.' in (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text()
guids = set()
for meta in (ROOT / 'Assets').rglob('*.meta'):
    guid = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.M).group(1)
    assert guid not in guids, f'Duplicate GUID: {meta}'
    guids.add(guid)
    assert Path(str(meta)[:-5]).exists(), f'Orphan metadata: {meta}'
for assembly in (ROOT / 'Assets').rglob('*.asmdef'):
    json.loads(assembly.read_text())
for number, count in [(1, 2), (2, 3), (3, 4)]:
    path = ROOT / f'Assets/_Game/ScriptableObjects/Levels/Level{number}.asset'
    text = path.read_text()
    # Remove Unity-specific YAML tag header; retain the actual serialized values.
    data = yaml.safe_load('\n'.join(line for line in text.splitlines() if not line.startswith(('%', '---'))))['MonoBehaviour']
    assert data['m_Script']['guid'] in guids
    assert data['levelNumber'] == number and data['difficulty'] == 0
    assert len(data['vehicles']) == count == data['expectedMoves']
    nodes = {n['id']: n for n in data['nodes']}
    assert len(nodes) == len(data['nodes'])
    cars = {v['id']: v for v in data['vehicles']}
    solution = data['intendedSolution']
    assert len(solution) == count and set(solution) == set(cars)
    for car in cars.values():
        assert car['speed'] > 0 and len(car['route']) >= 2
        assert all(n in nodes for n in car['route'])
        assert nodes[car['route'][-1]]['isExit']
        assert all(math.isfinite(v) for n in car['route'] for v in position(nodes[n]))
    pending = dict(cars)
    for entity_id in solution:
        car = pending.pop(entity_id)
        route = [position(nodes[n]) for n in car['route']]
        waiting = []
        for other in pending.values():
            a, b = (position(nodes[n]) for n in other['route'][:2])
            waiting.append((a, (.7, .425) if a[0] != b[0] else (.425, .7)))
        clear_against_waiting(route, waiting)
    if number >= 2:
        # A and C reach the common crossing together under simultaneous taps.
        a, c = cars['A'], cars['C']
        arrival = lambda v: math.dist(position(nodes[v['route'][0]]), position(nodes['X'])) / v['speed']
        assert abs(arrival(a) - arrival(c)) < 1e-9
    print(f'PASS Level {number}: {count} cars, valid exits, solution clearance' + (', simultaneous crossing conflict' if number >= 2 else ''))
print(f'PASS project integrity: {len(guids)} unique asset GUIDs; manifests and assemblies parse')
print('Unity compilation, physics tests, and Android build still require Unity Editor/toolchain.')
