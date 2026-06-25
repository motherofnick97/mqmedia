import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { ContractsRoutingModule } from './contracts-routing.module';
import { ContractsComponent } from './contracts.component';
import { CreateContractDialogComponent } from './create-contract/create-contract-dialog.component';
import { EditContractDialogComponent } from './edit-contract/edit-contract-dialog.component';
import { ManageContractKolsDialogComponent } from './contract-kols/manage-contract-kols-dialog.component';
import { AddContractKolDialogComponent } from './contract-kols/add-contract-kol-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        ContractsRoutingModule,
        ContractsComponent,
        CreateContractDialogComponent,
        EditContractDialogComponent,
        ManageContractKolsDialogComponent,
        AddContractKolDialogComponent,
    ],
})
export class ContractsModule {}
