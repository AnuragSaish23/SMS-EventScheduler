const { OPCUAServer, Variant, DataType } = require("node-opcua");

async function main() {
    const server = new OPCUAServer({
        port: 4840,
        resourcePath: "/UA/FactorySimulator",
        buildInfo: { productName: "SMS Factory Simulator", buildDate: new Date() }
    });

    await server.initialize();

    const addressSpace = server.engine.addressSpace;
    const ns = addressSpace.getOwnNamespace();
    const folder = ns.addFolder(addressSpace.rootFolder.objects, { browseName: "FactoryFloor" });

    // Sensor state
    let pumpVal = false, setupVal = false, completeVal = false;

        const pumpSensor = ns.addVariable({
        componentOf: folder, browseName: "PumpSensor", nodeId: "s=PumpSensor", dataType: "Boolean",
        value: { get: () => new Variant({ dataType: DataType.Boolean, value: pumpVal }) }
    });

    const setupSensor = ns.addVariable({
        componentOf: folder, browseName: "SetupSensor", nodeId: "s=SetupSensor", dataType: "Boolean",
        value: { get: () => new Variant({ dataType: DataType.Boolean, value: setupVal }) }
    });

    const completeSensor = ns.addVariable({
        componentOf: folder, browseName: "OperationComplete", nodeId: "s=OperationComplete", dataType: "Boolean",
        value: { get: () => new Variant({ dataType: DataType.Boolean, value: completeVal }) }
    });

    await server.start();
    console.log(`\n========================================`);
    console.log(`  SMS Factory Simulator is RUNNING`);
    console.log(`  Endpoint: ${server.getEndpointUrl()}`);
    console.log(`========================================\n`);
    console.log("Sensors will flip randomly every 10-30 seconds...\n");

    // Simulation loop: randomly flip sensors
    const simulate = () => {
        const rand = Math.random();

        if (rand < 0.35) {
            // Pump breaks or recovers
            pumpVal = !pumpVal;
            pumpSensor.setValueFromSource(new Variant({ dataType: DataType.Boolean, value: pumpVal }));
            console.log(`[${new Date().toLocaleTimeString()}] ${pumpVal ? "🔴 PUMP BROKE!" : "🟢 Pump fixed"} (PumpSensor = ${pumpVal})`);
        } else if (rand < 0.60) {
            // Setup change
            setupVal = !setupVal;
            setupSensor.setValueFromSource(new Variant({ dataType: DataType.Boolean, value: setupVal }));
            console.log(`[${new Date().toLocaleTimeString()}] ${setupVal ? "🟡 DIE CHANGE started" : "🟢 Die change done"} (SetupSensor = ${setupVal})`);
        } else {
            // Operation complete
            completeVal = !completeVal;
            completeSensor.setValueFromSource(new Variant({ dataType: DataType.Boolean, value: completeVal }));
            console.log(`[${new Date().toLocaleTimeString()}] ${completeVal ? "⚡ OPERATION COMPLETE signal" : "⏳ Waiting..."} (OperationComplete = ${completeVal})`);
        }

        // Next event in 10-30 seconds
        const nextDelay = 10000 + Math.floor(Math.random() * 20000);
        setTimeout(simulate, nextDelay);
    };

    // Start after 5 seconds
    setTimeout(simulate, 5000);
}

main().catch(console.error);
