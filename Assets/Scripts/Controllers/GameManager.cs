﻿using UnityEngine;
using System.Collections;

public enum Animation: byte { NOAMMO, CANATK, NODEF, CANDEF, NOREL, CANREL, NOTHING, DEATH, ATKHIT, ATKMISS, DEFHIT, DEFMISS, RELOK, RELFAIL, DEFFAIL, TUMBLEWEED}
public enum Result : byte { VICTORY, DEFEAT, DRAW }
public enum Action : byte { NOOP, ATK, DEF, REL , NOANSWER, NOCONNECTION}

// Recebe as mensagens do player e do inimigo conectado, avalia o comando nesse turno e envia o resultado calculado para os dois
public class GameManager : MonoBehaviour {

	enum State : byte { MENU, RESULT, TURN_START, WAITING, RESPONSE}

	private Player localPlayer, enemyPlayer;
	private GameObject[] playerObjects;
	private Timer timer;
	private GameObject timerObject;
	private State currentState;
	private GameObject canvasObject;
	private bool animating, playerAnimFinished, enemyAnimFinished, battleStarted;
	private float stopwatch;

	public bool battleEnded { get; private set; }
	public float endingDuration;
	
	public readonly int TRIES_LIMIT = 10;
	private int tries;
	public Connection connection;
	public SceneChanger sc;
	public GameObject playerPrefab, enemyPrefab, timerPrefab, victorySign, drawSign, defeatSignLeft, defeatSignRight, tumbleweed;

	private int maxDefenses;
	private int maxBullets;
	private float countdownTime, messageWaitTime;

	public int MaxDefenses {
		get { return maxDefenses; }
		set {
			if(!battleStarted) {
				maxDefenses = value;
			}
			else Debug.Log("Can not change that parameter during game");
		}
	}

	public int MaxBullets {
		get { return maxBullets; }
		set {
			if(!battleStarted) {
				maxBullets = value;
			}
			else Debug.Log("Can not change that parameter during game");
		}
	}

	public float CountdownTime {
		get { return countdownTime; }
		set {
			if (!battleStarted) {
				countdownTime = value;
			} else Debug.Log("Can not change that parameter during game");
		}
	}

	public void findCanvas() {
		canvasObject = GameObject.FindGameObjectWithTag("Canvas");
	}

	// Use this for initialization
	void Start () {
		// Define que o manager não deve ser destruído e qual o seu estado inicial
		DontDestroyOnLoad(gameObject);
		playerObjects = new GameObject[2];
		playerObjects[0] = playerObjects[1] = null;
		timer = null;
		maxBullets = 3;
		maxDefenses = 3;
		countdownTime = 3.0f;
		endingDuration = 5.0f;

		connection = null;
		battleStarted = false;
		battleEnded = true;
		currentState = State.MENU;
	}

	// Update is called once per frame
	void Update () {
		switch (currentState) {

		// Loop de batalha
		case State.TURN_START:
			// Verifica se o player já acabou sua animação e caso true armazena esse resultado
			if (!playerAnimFinished) {
				playerAnimFinished = localPlayer.FinishedAnimation;
			}
			// Verifica se a animação do inimigo já acabou
			if (!enemyAnimFinished) {
				enemyAnimFinished = enemyPlayer.FinishedAnimation;
			}
			// Caso as duas animações tenham acabado
			if (playerAnimFinished && enemyAnimFinished) {
				// Indica que o turno terminou
				playerAnimFinished = false;
				enemyAnimFinished = false;
				animating = false;
			}
			// Caso o turno tenha acabado e o timer esteja em 0
			if (timer.time <= 0 && !animating) {// Sempre que o timer não estiver ativo, uma ação deve ser realizada
				// Caso necessário, reinicia o timer
				if (!battleEnded) {
					timer.StartTimer(countdownTime, SelectAction); // inicia o timer
				// Senão, acaba a batalha
				} else {
					// Avalia qual foi o resultado
					Result result = localPlayer.alive ? 
										Result.VICTORY : 
										enemyPlayer.alive ? 
											Result.DEFEAT : 
											Result.DRAW;

					// Creates the sign to indicate battle result
					switch(result){
					case Result.VICTORY:
						GameObject.Instantiate(victorySign);
						break;
					case Result.DEFEAT:
						GameObject.Instantiate(defeatSignLeft);
						GameObject.Instantiate(defeatSignRight);
						break;
					case Result.DRAW:
						GameObject.Instantiate(drawSign);
						break;
					}
					// Changes current state
					currentState = State.RESULT;
				}
			}
			break;
        case State.WAITING:
            
            bool messageReceived = false;
            
            // Checks if enemy has sent action
            enemyPlayer.action = getEnemyAction();
                
            messageReceived = enemyPlayer.action != Action.NOANSWER && enemyPlayer.action != Action.NOCONNECTION;
            
            if (messageReceived) {
                // Waits for message to be received. It's possible to put a timeout counter here.
                // NOTE timeout is inside getEnemyAction but its a bad idea
                currentState = State.RESPONSE;
            } else if (enemyPlayer.action == Action.NOCONNECTION){
                GameObject.Instantiate(drawSign);
                currentState = State.RESULT;
            }
            break;
        case State.RESPONSE:
            // Executes the response base on received message
            // Tumbleweed
            if (localPlayer.action == Action.NOOP && enemyPlayer.action == Action.NOOP)
                GameObject.Instantiate(tumbleweed);
            // Realiza as ações selecionadas
            Animation localAnimation = localPlayer.DoAction();
            Animation enemyAnimation = enemyPlayer.DoAction();
            // Compara os Animations das duas ações e, de acordo com o que aconteceu chama as animações de reação
            localPlayer.DoReaction(localAnimation, enemyAnimation);
            enemyPlayer.DoReaction(enemyAnimation, localAnimation);
            // Se necessário, indica que acabou a batalha
            if (!localPlayer.alive || !enemyPlayer.alive)
                battleEnded = true;
            currentState = State.TURN_START;
            break;
		case State.RESULT:
			if (stopwatch > 0)
				stopwatch -= Time.deltaTime;
			else
				EndBattle();
			break;
		case State.MENU:
			break;
		default:
			break;
		}
	}

	// Após a conexão ser estabelecida e for verificado que ela está funcionando, inicia-se a batalha
	public void StartBattle() {

		stopwatch = endingDuration;
		findCanvas();

		// Instancia a prefab do player
		playerObjects[0] = GameObject.Instantiate(playerPrefab, gameObject.transform);
		
		// Obtem uma referencia para o script e configura
		localPlayer = playerObjects[0].GetComponent<Player>();
		localPlayer.Configure(maxDefenses, maxBullets);

		// Faz o mesmo para o inimigo
		playerObjects[1] = GameObject.Instantiate(enemyPrefab, gameObject.transform);
		enemyPlayer = playerObjects[1].GetComponent<Player>();
		enemyPlayer.Configure(maxDefenses, maxBullets);
		
		// Instancia e salva referencia para o timer
		timerObject = GameObject.Instantiate(timerPrefab);
		timerObject.transform.SetParent(canvasObject.transform, false);
		timer = timerObject.GetComponent<Timer>();
		
		// Set Bullets/Defences/Time BEFORE battleStarted = true
		this.connection.GetMessage();

		// Seta as variáveis booleanas que indicam o estado da batalha
		playerAnimFinished = false;
		enemyAnimFinished = false;
		animating = false;
		battleStarted = true;
		battleEnded = false;

		currentState = State.TURN_START;
	}

	// Função a ser chamada quando acabar a batalha
	public void EndBattle() {
		
		// Destrói os objetos e componentes desnecessários
		Destroy(playerObjects[0]);
		Destroy(playerObjects[1]);
		
		playerObjects[0] = playerObjects[1] = null;
		
		Destroy(timerObject);
		connection.CloseConnection();
		Destroy(connection);
		
		timerObject = null;
		connection = null;

		localPlayer = null;
		enemyPlayer = null;
		canvasObject = null;
		battleStarted = false;

		// Muda de cena
		currentState = State.MENU;
		sc.LoadMenuScene(false);
	}

	public void SetPlayerAction(Action selectedAction) {
		localPlayer.action = selectedAction;
	}

	private void SelectAction() {

		// Avisa que o turno começou
		animating = true;

		// Envia a ação selecionada
		sendLocalAction(localPlayer.action);
        messageWaitTime = 0;
        currentState = State.WAITING;
	}

	private Action getEnemyAction(){
		
		string message = null;
		this.tries++;

		connection.SetMessageType(connection.MyMsgType.Action);
		if(!connection.GetMessage(message))
			Debug.Log("Deu ruim");

		switch (message) {
		
		case "ATK":
			tries = 0;
			return Action.ATK;
		case "DEF":
			tries = 0;
			return Action.DEF;
		case "REL":
			tries = 0;
			return Action.REL;
		case "NOOP":
			tries = 0;
			return Action.NOOP;
		case "": // Empty
			tries = 0;
			return Action.NOOP;
		
		case null: // No message received, wait
			
			Debug.Log("[Debug] No message received. Tries: " + this.tries);
			System.Threading.Thread.Sleep(1); // FIXME: Should not use this
			if(tries > TRIES_LIMIT){
				Debug.Log("[Debug] Exceeded tries limit. Disconnecting...");
				connection.CloseConnection();
				return Action.NOCONNECTION; // Throw exception to disconnect?
			}
			return Action.NOANSWER;
		
		default: // Error
			Debug.Log("[Debug] Message: " + message);
			throw new System.Exception("Invalid message received");
		}
	}

	private void sendLocalAction(Action localAction) {
		switch (localAction) {
		case Action.ATK:
			connection.OtterSendMessage("ATK");
			break;
		case Action.DEF:
			connection.OtterSendMessage("DEF");
			break;
		case Action.REL:
			connection.OtterSendMessage("REL");
			break;
		case Action.NOOP:
			connection.OtterSendMessage("NOOP");
			break;
		default:
			throw new System.Exception("Trying to send invalid message");
		}
	}
}