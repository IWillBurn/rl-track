# RL-Track

RL-Track is a Unity + ML-Agents project where a racer agent learns to drive on procedurally generated tracks.  
The goal is to study generalization from a small set of training track patterns to a more complex unseen test track,  
using a combination of imitation learning (from demo) and reinforcement learning.

## Features

- Procedural track generation based on Unity Splines (track mesh, reward checkpoints, scattered objects).
- 6 different training track patterns and 1 unseen test track.
- Dense lidar-based observations for walls and checkpoints.
- Two-stage training: imitation-focused and reward-focused PPO.
- Ready-to-use training builds and configs for Linux and Windows.

---

## Tracks

The project includes an example of track-generating code (track mesh, reward checkpoints, randomly scattered objects along the track) based on Unity Splines, providing flexibility in creating and modifying tracks.

### Training tracks

There are 6 track patterns used for training:

1. Circular right turn  
2. Circular left turn  
3. Straight (direct) track  
4. Rectangular right turn  
5. Rectangular left turn  
6. Serpentine  

For each episode, one of these tracks is randomly selected.

### Test track

The test track is a composition of components of the training tracks, but the model is **never** trained on it.  
It is used only for evaluation and visual testing of generalization.

---

## Environment

Each environment instance is a looped highway-like track with checkpoints placed along the route.

### Checkpoints

- 40 checkpoints are located at equal distances along the track.
- The agent must collect checkpoints in the correct order.
- Missing or revisiting checkpoints is penalized indirectly through the reward structure and termination conditions.

### Reward function

The reward system is as follows:

1. **Time penalty**  
   - -0.05 per step.  
   - Encourages the agent to complete the route faster.

2. **Collision penalty**  
   - Additional -0.05 per step while the agent is colliding with the track boundaries.  
   - Encourages avoiding collisions with the boundaries.

3. **Checkpoint reward**  
   - +5 for each checkpoint collected in the correct order.

4. **Episode completion**  
   - +100 for completing the entire route (collecting all checkpoints).

5. **Stagnation penalty**  
   - If the agent does not reach a new checkpoint for more than 5 seconds, it receives -20 and the episode ends.  
   - Prevents the agent from standing still or looping in a small area.

---

## Agent

### Observations

At each step, the agent receives a stack of 5 frames to capture short-term dynamics. Each frame contains:

1. **Direction alignment**  
   - A scalar: the dot product between the agent's forward direction and the direction to the next checkpoint.

2. **Lidar: walls**  
   - 16 lidar rays measuring distances to the track boundaries.

3. **Lidar: checkpoints**  
   - 16 lidar rays measuring distances to checkpoints.  
   - Each ray also encodes the type of checkpoint it “sees” (new / already collected).

To capture dynamics, 5 such frames are stacked and passed to the model as the observation.

> Exact tensor shape and encoding details can be seen in the Unity environment and ML-Agents behavior configuration.

### Actions

At each step, the agent outputs 2 discrete integer actions in the range `[0, 10]`:

1. **Speed control**  
   - Maps to a change in driving speed from `-5 … 0 … +5`.

2. **Steering control**  
   - Maps to a change in turning from `-5 … 0 … +5`.

In ML-Agents terms, this corresponds to two discrete action branches with 11 possible values each.

---

## Demo

For training, a demonstration recording of driving along the training tracks is used:

- 65 episodes  
- 40.470 steps  
- Average reward ≈ 264 points  

The demo was recorded on the 6 training tracks and is used for imitation learning in the first training stage.  
The demo file is available in the `/train` release and referenced in the training configs.

---

## Training

Training uses PPO from ML-Agents and is performed in two stages:

1. **Imitation-focused stage**  
   - Priority is given to matching the behavior from the demo recording.  
   - The agent is strongly regularized towards the demonstration trajectories.

2. **RL-focused stage**  
   - The influence of the demo is reduced.  
   - Behavior is shaped mainly by the environment reward (interaction with the environment).

The configs for each stage can be found in `export/config/` and in the `/train` release:

- Stage 1: `export/config/racer-ppo.yaml`  
- Stage 2: `export/config/racer-ppo-r1.yaml` (initialized from the first stage)

ML-Agents logs are saved under `results/` by default, with subfolders matching `--run-id`.

---

## Results

The results of training (PPO statistics, rewards, etc.) are available in TensorBoard format  
and can be viewed in the `/results` release.

To visualize locally, use:

```bash
tensorboard --logdir=./results/ --port=6006
```

Then open `http://localhost:6006` in your browser.

---

## Testing

You can see the performance of the trained model on the **unseen test track** by running the corresponding build  
from the `/tests` release.

This build runs the trained agent and demonstrates its ability to drive on a more complex composite track built from  
the elements of the training tracks.

---

## Artifacts
Release:
- Training builds, configs and demo: `/train` in release  
- Training results: `/results` in release  
- Test builds: `/tests` in release  

---

## Requirements
- Python >= 3.10 and < 3.11  
- ML-Agents 1.1.0  
- PyTorch ~= 2.2.1  
- Unity environment builds from `export/builds/`
- (Optional) TensorBoard for viewing training results

---

## How to Run Training

Run these commands from the root of the repository.

### 1. Create and activate virtual environment

```bash
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
python -m pip install --upgrade pip
pip install "torch~=2.2.1"  # For GPU, you can use: --index-url https://download.pytorch.org/whl/cu121
pip install mlagents==1.1.0
```

### 2. Start training

#### Linux

```bash
mlagents-learn export/config/racer-ppo.yaml --run-id=racer-ppo --env=export/builds/train-linux/rl-track.x86_64 --no-graphics
mlagents-learn export/config/racer-ppo-r1.yaml --run-id=racer-ppo-r1 --initialize-from=racer-ppo --env=export/builds/train-linux/rl-track.x86_64 --no-graphics
```

#### Windows

```bash
mlagents-learn export/config/racer-ppo.yaml --run-id=racer-ppo --env=export/builds/train-win/rl-track.exe --no-graphics
mlagents-learn export/config/racer-ppo-r1.yaml --run-id=racer-ppo-r1 --initialize-from=racer-ppo --env=export/builds/train-win/rl-track.exe --no-graphics
```

After training, check the `results/` directory for logs and run TensorBoard if needed.

---